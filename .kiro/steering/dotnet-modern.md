---
inclusion: fileMatch
fileMatchPattern: "{.kiro/skills/generate-plans/references/**,working/*/400-mod-plan/**,working/*/300-detailed-assessment/**}"
---

# .NET Modernization on AWS — Authentication, Session, and Data Protection

Reference guide for modernizing Windows .NET applications (Web Forms, MVC, Classic ASP) to ASP.NET Core on ECS Fargate Linux. Covers the authentication and session management patterns that come up in nearly every engagement.

## User Authentication: Entra ID OIDC

When migrating from Windows Authentication (Kerberos/NTLM via IIS), the recommended replacement is **Entra ID with OpenID Connect (OIDC)**. Most enterprise customers already have Entra ID with AD sync in place.

### How OIDC works (Authorization Code + PKCE flow)

1. User hits the app → app redirects browser to Entra's `/authorize` endpoint with client ID, redirect URI, scopes (`openid profile email`), and PKCE challenge.
2. Entra shows login (or SSO kicks in silently for domain-joined devices). User authenticates.
3. Entra redirects back to the app's callback URL with an **authorization code** in the query string.
4. App backend exchanges the code (+ PKCE verifier) at Entra's `/token` endpoint.
5. Entra responds with three tokens:
   - **ID token** — JWT containing identity claims (who the user is)
   - **Access token** — JWT authorizing API calls to downstream services
   - **Refresh token** — used to get new tokens without re-prompting

All three are JWTs (base64-encoded, three-part `header.payload.signature` strings). The ID token always contains: `iss`, `sub`, `aud`, `exp`, `name`, `preferred_username`, `oid`, `tid`, and optionally `roles` and `groups`.

### Where the app handles OIDC (not the ALB)

Two patterns exist for ALB + OIDC:

| Pattern | How it works | When to use |
|---------|-------------|-------------|
| **ALB-native OIDC** | ALB handles the OIDC dance, forwards `x-amzn-oidc-data` headers to the container | Quick auth gate in front of a legacy app you can't modify. Basic "is this person in our tenant?" check. |
| **App handles OIDC** (recommended) | ALB is plain TLS termination. App uses `Microsoft.Identity.Web` for the full OIDC flow. | Modern intranet apps. Full control over claims, roles, group mappings, token refresh, downstream API calls. |

**Default recommendation: app handles OIDC.** Reasons:
- Richer claim handling — request specific scopes, call downstream APIs with the access token
- Role/group-based authorization maps directly to .NET middleware (`[Authorize(Roles = "Admin")]`)
- Portable — app works the same behind any load balancer or locally in dev
- Proper token refresh — no full redirect on session expiry

### Authorization after OIDC

Most legacy .NET apps don't use AD groups directly for authorization. They authenticate the user (get their UPN or domain username), then look up roles/permissions from a database table. This pattern carries forward:

1. User authenticates via Entra ID OIDC → app receives UPN (e.g., `user@contoso.com`)
2. App queries existing database: UPN → Employee ID → roles/permissions
3. App uses the DB-sourced roles for authorization decisions

This avoids complex Entra ID group/role claim configuration and preserves the customer's existing authorization model.

## Downstream Authentication: App → SQL Server

Three options for how the application authenticates to SQL Server from a Linux container:

| Option | Mechanism | Complexity | When to use |
|--------|-----------|-----------|-------------|
| **SQL mixed-mode auth via Secrets Manager** (recommended) | SQL login (username/password) stored in Secrets Manager. Fargate task assumes IAM role to read the secret. | Low | Default choice. Simple, no AD dependency at runtime. Works when the app already uses SQL auth (most legacy .NET apps do). |
| **gMSA (Group Managed Service Account)** | Windows credential-based auth. Requires Credentials Fetcher, credspec, AD domain controllers accessible from ECS. | High | Only when the customer requires Windows Integrated Auth to SQL Server and cannot switch to SQL auth. Requires Windows containers or Linux with Credentials Fetcher (preview). Adds significant infrastructure complexity. |
| **IAM database authentication** | IAM role generates short-lived auth tokens for Aurora PostgreSQL/MySQL. | Medium | Only for Aurora. Not applicable when staying on SQL Server. Preferred for Aurora PostgreSQL post-migration. |

**Default recommendation: SQL auth via Secrets Manager.** Reasons:
- Most legacy .NET apps already use SQL auth (connection string with username/password in web.config)
- Zero AD dependency at runtime — no domain controllers, no gMSA, no Credentials Fetcher
- Secrets Manager handles rotation via Lambda rotation function (post-EBA concern)
- IAM role scoping ensures only the specific ECS task can read the specific secret

**When gMSA is unavoidable:** If the customer's SQL Server is configured for Windows Authentication only (no SQL logins) and they cannot or will not enable mixed-mode auth, then gMSA is required. This is rare but does happen in tightly locked-down environments. gMSA adds:
- Dependency on AD domain controllers accessible from the ECS VPC
- Credentials Fetcher sidecar or agent on the container host
- credspec JSON file in the ECS task definition
- Windows containers (or Linux with Credentials Fetcher, which is in preview and not production-ready for all scenarios)

Flag gMSA as a PoC candidate whenever it appears — it's a high-risk unknown that must be validated before committing to an EBA.

## Data Protection Keys

ASP.NET Core's Data Protection API is the framework's built-in system for encrypting sensitive data — authentication cookies, anti-forgery tokens, and session cookies. It replaces the `<machineKey>` from Web Forms.

### What the key is

A symmetric encryption key (AES-256-CBC + HMACSHA256) that the framework generates automatically. It encrypts auth cookies before sending them to the browser and decrypts them when they come back. The key is stored as an XML file.

### Why it matters for containers

On a traditional IIS server, the key persists on disk for the life of the server. In containers:

- Container starts → key generated in memory/local filesystem
- Container dies → key is gone
- New container starts → new key → old cookies can't be decrypted → users logged out

This is the same problem `machineKey` solved in Web Forms — but Data Protection API handles it with automatic key management instead of manually pasting hex strings into web.config.

| Concept | Web Forms (old) | ASP.NET Core (new) |
|---------|----------------|-------------------|
| Encryption key | `<machineKey>` in web.config | Data Protection key (auto-generated XML) |
| What it encrypts | ViewState, Forms Auth ticket, session | Auth cookies, anti-forgery tokens, session cookies |
| Shared across servers | Copy/paste same machineKey to all servers | Persist keys to shared store (Valkey, S3, SSM) |
| Key rotation | Manual (never, in practice) | Automatic (new key every 90 days, old keys kept for decryption) |

### Sharing keys across container instances

For any deployment with more than one container (or where containers restart), Data Protection keys must be stored in a shared location. Without this, Container A encrypts a cookie that Container B can't decrypt — forcing sticky sessions or random auth failures.

**Recommended: ElastiCache Valkey** (when the customer is already using Valkey for session state — one store for both concerns). Alternatives: SSM Parameter Store (simplest if no Valkey), S3 bucket.

## Session State and Horizontal Scaling

### The problem

ASP.NET Core's default in-memory session works for a single container. With multiple containers or container restarts:

- User authenticates on Container A → session stored in Container A's memory
- Container A dies → ECS replaces with Container B → session is gone → user experience breaks
- Even without failure: ALB round-robins requests → user hits Container B → no session → broken

Sticky sessions (ALB session affinity) mask this but defeat horizontal scaling and graceful failover.

### The solution: ElastiCache Valkey

Externalize both session state and Data Protection keys to ElastiCache Valkey. Valkey is a drop-in Redis replacement — the standard `StackExchangeRedis` NuGet packages work as-is.

At startup, the app wires two things against the Valkey connection string:

1. **Data Protection** — persist keys to Valkey under a shared ring name (e.g. `DataProtection-Keys`) and set a stable application name so all containers share the same ring.
2. **Distributed session** — register the Redis/Valkey distributed cache, then enable session with the usual cookie hardening (HttpOnly, Secure, sensible idle timeout).

Pseudocode:

```
configure DataProtection:
    persist keys to Valkey at "DataProtection-Keys"
    application name = "<app-name>"   // must match across all containers

configure DistributedCache:
    provider   = Redis/Valkey
    connection = <valkey-connection-string>

configure Session:
    idle timeout         = 30 min
    cookie HttpOnly      = true
    cookie SecurePolicy  = Always
```

**Result:** Any container can serve any request. Container restarts don't lose sessions. No sticky sessions. Horizontal scaling works out of the box.

### When Valkey is required vs optional

| Scenario | Valkey needed? |
|----------|---------------|
| EBA pilot, single container, low concurrency | Optional (in-memory works) but recommended to validate the pattern |
| Production, multiple containers | Required |
| Any deployment where containers restart (ECS Fargate — always) | Required for good UX |
| Customer already using Valkey for caching | Use the same cluster — no additional infrastructure |

**Default recommendation:** Include Valkey from day one in the EBA. It's 5 lines of config, two NuGet packages, and avoids the "works in dev, breaks in production" trap. If the customer is already proposing Valkey for caching, this is a no-brainer.

## Target Architecture Pattern

The standard architecture for a modernized .NET intranet app on AWS:

```
User → Entra ID (OIDC) → ALB (HTTPS) → ECS Fargate Linux (.NET 10)
                                              ↕                ↕
                                         SQL Server      ElastiCache Valkey
                                      (via Secrets Mgr)  (session + DP keys)
```

- ALB handles TLS termination only — no auth at the ALB level
- App handles OIDC via `Microsoft.Identity.Web`
- SQL credentials in Secrets Manager, accessed via IAM role on the ECS task
- Session state and Data Protection keys in Valkey
- No sticky sessions, no Windows containers, no AD dependency at runtime

## PoC Checklist

When authentication is a blocker (Windows Auth → OIDC migration), validate with a 2-week PoC before the EBA:

1. Entra ID OIDC flow works (redirect, token exchange, claims)
2. Domain user → Employee ID lookup works (UPN from token → DB query → roles)
3. SQL Server connectivity via Secrets Manager works (IAM role → secret → connection)
4. Data Protection keys shared via Valkey (multiple containers can decrypt each other's cookies)
5. Session state survives container restart (kill container, verify session persists)
6. Auth works across multiple containers without sticky sessions (scale to 2, verify cross-instance auth)
7. Pattern is reusable (second app config with different Entra ID app registration)
