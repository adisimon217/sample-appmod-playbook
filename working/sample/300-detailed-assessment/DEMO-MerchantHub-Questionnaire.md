# .NET & SQL Server Modernization — Pre-EBA Discovery Questionnaire

**Application:** MerchantHub (APP-002)
**Customer:** Voyager Pte Ltd
**Pre-populated:** April 2026 (from Phase 100 inventory + Phase 200 feasibility)
**Updated:** April 2026 (Voyager review — Priya, Marcus, Wei Lin)
**Status:** Review complete — 80% answered, remaining gaps flagged

**Target State:** .NET application containerized on Amazon ECS (Fargate/Linux) + SQL Server migrated to Amazon Aurora PostgreSQL.

---

## 1. Application Overview

1.1. `[Per app]` Application name:
> MerchantHub

1.2. `[Per app]` Business owner / stakeholder:
> David Tan, VP Merchant Services. Priya Sharma (Head of Engineering) is technical owner.

1.3. `[Per app]` Brief description — what does the application do?
> Merchant self-service portal for viewing transactions, downloading settlement reports, managing account details, and submitting dispute responses. Used by ~3000 active retail merchants across SEA (SG, MY, TH, ID, PH). Merchants log in daily to reconcile their transactions against bank settlements.

1.4. `[Per app]` Business criticality (Critical / High / Medium / Low):
> High. Merchant-facing — downtime generates support tickets and erodes merchant trust. Not revenue-critical like PayGate (merchants can still process payments during MerchantHub downtime), but prolonged outages risk merchant churn.

1.5. `[Per app]` Primary users — internal, external, or both? Estimated number of active users and their locations?
> External (merchants) + small internal admin team (~5 ops staff). ~200 average / ~800 peak concurrent merchant sessions. Peak hours: 9am-12pm SGT weekdays, month-end spike (1st-3rd of each month). Locations: Singapore (40%), Malaysia (25%), Thailand (15%), Indonesia (12%), Philippines (8%).

1.6. `[Per app]` What happens during downtime? Maximum tolerable downtime?
> Merchants cannot view transactions, download reports, or respond to disputes. Support team receives ~50 calls/hour during outages. Tolerable downtime: 2 hours during business hours, 8 hours overnight. No formal SLA with merchants today but David wants to commit to 99.9% once on AWS.

1.7. `[Per app]` What is the application's strategic importance and expected remaining lifetime? Any plans to replace or sunset it?
> Core merchant relationship tool — no plans to replace. David wants to add real-time transaction notifications and a merchant mobile app in 2027. Modernization is a prerequisite for both.

1.8. `[Per app]` Any compliance, regulatory, or data-residency constraints?
> PCI-DSS Level 1 (Voyager processes >6M transactions/year). MerchantHub does NOT store full card numbers — it displays masked PANs (last 4 digits) retrieved from PayGate's tokenized transaction store. Merchant PII (names, addresses, bank account details) must remain in Singapore or Malaysia per MAS guidelines. No cross-border data transfer to regions outside SEA without approval.

1.9. `[Per app]` Is there existing documentation we can review? (Architecture diagrams, runbooks, API specs — please share)
> Partial. Wei Lin (senior dev) has an informal wiki with API endpoint docs and deployment notes. No formal architecture diagrams. Whiteboard from kickoff meeting is the best high-level view. Wei Lin will export the wiki before the architecture workshop.


---

## 2. Application Architecture & Technology Stack

2.1. `[Per project]` Any of these application/project types in the solution? (Yes/No for each)
- ASP.NET Web Forms: No
- ASP.NET MVC: Yes (main web project)
- ASP.NET Web API: Yes (internal API project for merchant data endpoints)
- WCF (Windows Communication Foundation): No
- WPF (Windows Presentation Foundation): No
- XBAP (XAML Browser Application): No
- Windows Workflow Foundation (WF): No
- Windows Service: No
- Console Application: Yes (one-off data migration utility, not production)
- Blazor / SPA (Angular, React): No (jQuery + Bootstrap front-end)
- gRPC / Worker Service: No
- Class Library: Yes (3 — MerchantHub.Core, MerchantHub.Data, MerchantHub.Reports)

2.2. `[Per app]` Architecture style (Monolithic / SOA / Microservices / N-Tier):
> N-Tier monolith. Single solution with MVC web layer, API layer, business logic (Core), data access (Data), and reporting (Reports). All deployed as one IIS application.

2.3. `[Per project]` Language(s) used (e.g., C#, VB.NET, F#):
> C# throughout. Some inline JavaScript/jQuery in Razor views.

2.4. `[Per project]` .NET Framework / .NET version (e.g., .NET Framework 4.5.2, .NET Framework 4.8, .NET 6, .NET 8):
> .NET Framework 4.8 (all projects). Upgraded from 4.6.1 about 18 months ago.

2.5. `[Per app]` Number of solutions (.sln) and projects (.csproj / .vbproj). If multiple, briefly describe the purpose of each project:
> 1 solution, 7 projects:
> - MerchantHub.Web (ASP.NET MVC — controllers, views, static assets)
> - MerchantHub.Api (ASP.NET Web API — merchant data endpoints, used by Web project via AJAX)
> - MerchantHub.Core (class library — business logic, domain models, validation)
> - MerchantHub.Data (class library — EF DbContext, repositories, migrations)
> - MerchantHub.Reports (class library — Crystal Reports wrapper, report generation)
> - MerchantHub.Common (class library — shared utilities, constants, extensions)
> - MerchantHub.Tests (unit tests — MSTest, ~15% coverage)

2.6. `[Per project]` Estimated lines of code — overall and per major project if known:
> ~58K LoC total (cloc output):
> - MerchantHub.Web: 22K (controllers + Razor views)
> - MerchantHub.Api: 8K
> - MerchantHub.Core: 12K
> - MerchantHub.Data: 9K
> - MerchantHub.Reports: 4K
> - MerchantHub.Common: 2K
> - MerchantHub.Tests: 1K

2.7. `[Per app]` What is the team's preferred modernization approach?
> Port existing code to .NET 10. Wei Lin's team does not want a full rewrite — they want to preserve the existing business logic and MVC structure. Open to replacing Crystal Reports with something modern.

2.8. `[Per app]` How would you rate the current tech debt level?
> Moderate. Some areas of concern: (a) Fat controllers — business logic mixed into MVC controllers instead of Core layer, (b) raw SQL queries in a few repositories bypassing EF, (c) Crystal Reports integration is brittle and hard to test, (d) no dependency injection — static service locator pattern. No formal tech debt registry.

2.9. `[Per component]` Any of these Windows-OS or IIS-specific dependencies in use? (Yes/No for each)
- COM / COM+ / Interop components: Yes — Crystal Reports runtime uses COM interop
- OLE / Office Interop (e.g., Excel/Word automation): No
- P/Invoke calls to native DLLs: No
- Windows Registry: No
- Global Assembly Cache (GAC): Yes — Crystal Reports assemblies registered in GAC
- MSMQ / Windows Event Log: No
- IIS URL Rewrite rules: Yes — merchant-friendly URLs, HTTP-to-HTTPS redirect, legacy URL redirects
- Custom IIS modules or ISAPI filters: No
- IIS virtual directories or sub-applications: No
- Windows Authentication via IIS (Negotiate/NTLM): Not currently, but planned for internal admin area post-modernization (David wants AD integration for ops staff). Will need Negotiate/Kerberos or OIDC federation against on-prem AD.
- web.config handlers, machine keys, or httpModules: Yes — machine key for forms auth ticket encryption (shared across load-balanced servers... but they only have one server today)

2.10. `[Per app]` Does the application store session state or use cookies? Where is state stored?
> InProc session state (ASP.NET default). Used for: merchant login session, shopping cart-like state for dispute submissions, report generation parameters. Forms authentication cookie (encrypted, 30-min sliding expiration). Session timeout: 20 minutes.

2.11. `[Per app]` Does the application have tightly coupled dependencies between modules?
> Yes. MerchantHub.Data directly references PayGate's database tables (shared PaymentsDB). There are ~15 EF entity classes that map to PayGate-owned tables (Transactions, Settlements, Merchants, MerchantAccounts). MerchantHub has its own tables too (~40 tables for disputes, report configs, user preferences, audit logs). No shared code libraries with PayGate — they are separate solutions.


---

## 3. Configuration, Caching & Storage

3.1. `[Per app]` How is application configuration managed?
> web.config with config transforms per environment (Dev, UAT, Prod). Connection strings in web.config (not encrypted — Marcus flagged this as a security concern). AppSettings for feature flags, API keys, SMTP config. No external config server.

3.2. `[Per component]` Does the application write to local disk?
> Yes:
> - Crystal Reports generates PDFs to `D:\MerchantHub\Reports\` (temp files, cleaned up after 24 hours). ~500 MB/day during month-end, ~50 MB/day otherwise.
> - Log files written to `D:\MerchantHub\Logs\` via log4net. ~200 MB/day, rotated weekly.
> - Merchant file uploads (dispute evidence — photos, PDFs) saved to `D:\MerchantHub\Uploads\`. ~2 GB total, growing ~100 MB/month.

3.3. `[Per app]` Any caching in use?
> In-memory MemoryCache for: merchant profile data (5-min expiry), transaction summary aggregations (1-min expiry), static reference data (currency codes, country lists — 1-hour expiry). No distributed cache.

3.4. `[Per app]` What logging framework is in use?
> log4net. Logs to rolling file appender (daily rotation, 7-day retention). Log levels: INFO in prod, DEBUG in dev/UAT. No structured logging — plain text format.


---

## 4. Dependencies & Integrations

4.1. `[Per project]` NuGet packages — public or private feed?
> Public NuGet.org only. Key packages: EntityFramework 6.4.4, Newtonsoft.Json 13.0.3, log4net 2.0.15, AutoMapper 12.0.1, CrystalDecisions.CrystalReports (v13.0.35 — SAP runtime). No private NuGet feeds.

4.2. `[Per project]` Any paid or third-party UI/component libraries in use?
> (a) SAP Crystal Reports for .NET, v13.0.35 (runtime). (b) Used in MerchantHub.Reports project only. (c) No ASP.NET Core edition exists — Crystal Reports is .NET Framework only. (d) Team is willing to replace — Wei Lin prefers a modern PDF generation approach. No other paid UI libraries — front-end is jQuery 3.6 + Bootstrap 4.6 + DataTables.net (all open source).

4.3. `[Per app]` Any internal / shared libraries?
> No shared libraries with other Voyager applications. MerchantHub is self-contained. PayGate integration is via shared database, not shared code.

4.4. `[Per component]` Does the application expose APIs?
> MerchantHub.Api exposes REST endpoints consumed by the MVC front-end via AJAX (same-origin). Not consumed by external systems. ~25 API endpoints for transaction search, report generation, dispute management, merchant profile.

4.5. `[Per component]` Does the application consume external APIs or services?
> No external API consumption. All data comes from PaymentsDB (shared with PayGate). SMTP is the only outbound integration (see 4.7).

4.6. `[Per component]` Internal integration points:
> - PaymentsDB (shared SQL Server) — direct EF queries against PayGate-owned tables (read-only for transactions/settlements, read-write for MerchantHub-owned tables)
> - No message queues, file shares, or FTP

4.7. `[Per component]` External integration points:
> - SMTP: on-prem Exchange server (mail.voyager.internal) for merchant notifications (transaction alerts, dispute status updates, monthly statement emails). ~2000 emails/day. Uses System.Net.Mail.SmtpClient.
> - No other external integrations.

4.8. `[Per component]` Any downstream applications dependent on this application?
> No. MerchantHub is a leaf node — nothing depends on it. Merchants access it via browser only.

4.9. `[Per app]` Does the application use Crystal Reports / SSRS / other reporting tools?
> Crystal Reports v13.0.35 (SAP runtime). 12 report templates: monthly merchant statement (most used), daily transaction summary, settlement reconciliation report, dispute status report, 8 others (Wei Lin says 4 of the 8 are rarely used). Reports generated on-demand when merchant clicks "Download" — PDF output. ~150 reports/day average, ~800/day month-end. No SSRS in MerchantHub (SSRS is used by ComplianceReporter).


---

## 5. Batch Jobs & Background Processing

5.1. `[Per component]` Are there Windows Services or console-based batch applications?
> No dedicated Windows Services. However, there is a Hangfire background job processor running in-process within the IIS app pool. Jobs: (a) nightly merchant statement generation (pre-generates PDFs for top 500 merchants), (b) hourly transaction summary cache refresh, (c) daily cleanup of expired report temp files.

5.2. `[Per component]` Are they scheduled (how often?) or running 24/7?
> Hangfire scheduler runs 24/7 within IIS. Nightly statement generation: 2am SGT. Hourly cache refresh: every 60 minutes. Daily cleanup: 4am SGT. Hangfire dashboard accessible at /hangfire (IP-restricted to office network).

5.3. `[Per component]` How long does a typical batch run last?
> Nightly statement generation: 45 minutes (500 merchants x 12 pages avg). Hourly cache refresh: 2-3 minutes. Daily cleanup: < 1 minute.

5.4. `[Per component]` What data volumes does each batch job process per run?
> Statement generation: reads ~2M transaction records across 500 merchants, generates ~500 PDFs (~3 MB each, ~1.5 GB total). Cache refresh: aggregates ~50K recent transactions.

5.5. `[Per component]` How are batch jobs monitored?
> Hangfire dashboard shows job status. No alerting — Wei Lin checks manually if merchants complain about missing statements. ⚠️ _Gap: no automated alerting on job failures._

5.6. `[Per component]` What happens when a batch job fails?
> Hangfire retries 3 times with exponential backoff. If all retries fail, the job is marked as failed in the dashboard. No notification. Partial failures (e.g., 3 of 500 statements fail) are not handled — the failed merchants don't get pre-generated statements and fall back to on-demand generation.

5.7. `[Per component]` Dependencies on other jobs or external triggers:
> Statement generation depends on PayGate's nightly settlement batch completing first (runs at 1am, usually done by 1:30am). No formal dependency — just a 30-minute buffer.

5.8. `[Per component]` Do any batch jobs monitor or read/write to Windows file shares?
> No UNC paths. All file I/O is local disk (D:\MerchantHub\Reports\).


---

## 6. Authentication & Security

6.1. `[Per app]` Authentication method:
> Dual authentication model (updated per David Tan's direction during architecture workshop):
>
> **External users (merchants):** ASP.NET Forms Authentication with custom membership provider backed by SQL Server (MerchantHub-owned tables in MerchantHubDB: MH_Users, MH_Roles, MH_UserRoles). Merchants register with email + password. Passwords hashed with bcrypt. No MFA today — David wants to add it post-modernization.
>
> **Internal users (ops staff):** Currently also use forms auth with an "Admin" role (same MH_Users table). David wants internal staff to authenticate via Windows Auth (Active Directory) going forward. This aligns with BackOffice and PayGate admin endpoints which already use AD. On-prem AD DCs are reachable from AWS VPC via existing VPN (confirmed in architecture workshop). Target: integrate AD via Negotiate/Kerberos or OIDC federation for the internal admin area, while keeping the existing merchant login flow unchanged.

6.2. `[Per app]` Authorization model:
> Role-based. 3 roles: Merchant (view own transactions, download reports, submit disputes), MerchantAdmin (manage sub-users for their merchant account), Admin (internal ops — view all merchants, manage disputes, generate cross-merchant reports). Implemented via custom AuthorizeAttribute on controllers. Post-modernization, Admin role will be sourced from AD group membership instead of the MH_UserRoles table.

6.3. `[Per app]` Any encryption requirements beyond TLS?
> TLS 1.2 enforced for all traffic (F5 terminates SSL). No field-level encryption in the database — masked PANs are stored as-is (last 4 digits only, not reversible). Merchant bank account details (for settlements) are stored in plain text in the DB. ⚠️ _Marcus flagged this as a PCI concern — should be encrypted at rest at minimum._


---

## 7. Hosting, Scaling & Resilience

7.1. `[Per app]` How is the application currently hosted?
> Single Windows Server 2019 VM in Equinix SG1. IIS 10. Not containerized. The VM also hosts the Hangfire background jobs (in-process). No load balancer for MerchantHub specifically — F5 routes traffic directly to the single server.

7.2. `[Per instance]` Hardware specs per instance:
> 8 vCPUs, 32 GB RAM, 500 GB SSD (D: drive for reports/logs/uploads). VM runs on VMware vSphere cluster. CPU utilization: ~30% average, ~70% during month-end statement generation.

7.3. `[Per app]` Traffic pattern:
> Known peaks. Weekday business hours (9am-6pm SGT) are steady. Month-end spike (1st-3rd): 3-4x normal traffic as merchants download statements and reconcile. Year-end (December): additional spike from annual reporting.

7.4. `[Per app]` Is the application highly available today?
> No. Single server, no failover. Last outage: 6 weeks ago, IIS app pool recycled during month-end peak and took 10 minutes to recover. Before that, a Windows Update reboot caused 45 minutes of unplanned downtime.

7.5. `[Per app]` SLA, RPO, and RTO targets:
> No formal SLA today. Target post-modernization: 99.9% availability, RTO 15 minutes, RPO 5 minutes. David wants to commit this to merchants in the updated service agreement.

7.6. `[Per app]` Monitoring and alerting tools in use:
> Basic: Windows Performance Monitor for CPU/memory, IIS logs analyzed manually. Sarah set up a simple uptime check (ping every 5 minutes) via an internal Nagios instance. No APM, no distributed tracing, no log aggregation.

7.7. `[Per app]` Can the full application stack be loaded in an isolated sandbox environment?
> ⚠️ _Not currently. MerchantHub requires PaymentsDB which is shared with PayGate. Marcus can provision a restored backup of PaymentsDB on a separate SQL Server instance for dev/test — he estimates 2 days to set up. The SMTP dependency can be mocked (Wei Lin has done this before with Papercut). No other external dependencies to mock._


---

## 8. Testing & Code Quality

8.1. `[Per project]` Are there unit or integration tests in place? What framework?
> MSTest. MerchantHub.Tests project has ~120 unit tests covering Core business logic (validation rules, calculation helpers). No integration tests. No tests for controllers, API endpoints, or data layer.

8.2. `[Per project]` Approximate unit test code coverage percentage:
> ~15% overall. Core project is ~40% covered. Web, Api, Data, Reports projects have 0% coverage.

8.3. `[Per app]` Are there load, performance, security, or end-to-end tests in place?
> No load tests. No security tests. Wei Lin occasionally runs manual smoke tests in UAT before deployments. No automated end-to-end tests.

8.4. `[Per app]` How often are tests run?
> Manually by developers before committing. Not integrated into CI pipeline. ⚠️ _Gap: tests are not enforced — developers sometimes skip them._


---

## 9. DevOps & Environments

9.1. `[Per app]` Source control system:
> Git / GitHub (private repo under Voyager org). Main branch + feature branches. No branch protection rules.

9.2. `[Per app]` CI/CD pipeline in place? What tool?
> Semi-automated. Jenkins (shared instance with PayGate). Build triggered on push to main. Jenkins builds the solution, runs MSTest tests, produces a deployment package (.zip). Deployment to IIS is manual — Wei Lin RDPs into the server and runs a PowerShell script that stops the app pool, copies files, starts the app pool.

9.3. `[Per app]` Deployment process:
> Semi-automated. Jenkins builds, human deploys. Typical deployment: Wei Lin merges PR to main, Jenkins builds (~3 min), Wei Lin downloads artifact, RDPs to prod server, runs deploy script (~5 min). Rollback: Wei Lin keeps the previous deployment package and can restore manually (~10 min).

9.4. `[Per app]` Environments in use:
> 3 environments: Dev (Wei Lin's laptop + shared SQL Server dev instance), UAT (separate VM, same spec as prod, shared PaymentsDB UAT copy — refreshed monthly), Prod. Dev and UAT are not identical to prod — Dev runs SQL Server 2019 Developer Edition, UAT has a smaller DB (last month's snapshot, ~200 GB vs 850 GB prod).

9.5. `[Per app]` How often is new code deployed to production?
> Weekly — typically Thursday afternoons (SGT). Hotfixes as needed (~2 per month).

9.6. `[Per team]` Any existing container or Linux experience on the team?
> Minimal. Sarah (DevOps) completed a Docker fundamentals course last month. Wei Lin has experimented with Docker locally but never deployed containers to production. No Linux administration experience on the team. Priya has budgeted for AWS container training (ECS/Fargate) as part of the modernization.


---

## 10. Team & Readiness

10.1. `[Per app]` Application developed/maintained in-house or by a vendor?
> In-house. Originally built 6 years ago by a team of 4. Current team is smaller.

10.2. `[Per app]` Is the full source code available and accessible?
> Yes — full source in GitHub. No binary-only dependencies except Crystal Reports runtime.

10.3. `[Per app]` Number of developers actively working on this application:
> 2 full-time: Wei Lin (senior dev, 4 years on MerchantHub) and Aisha (mid-level dev, 1 year). Wei Lin also supports PayGate ~20% of the time.

10.4. `[Per team]` Team's .NET proficiency level:
> Advanced (Wei Lin), Intermediate (Aisha). Both are C# / ASP.NET MVC experienced. Wei Lin has done some .NET 8 side projects. Aisha has only worked with .NET Framework.

10.5. `[Per team]` Team's experience with PostgreSQL:
> None. Both developers have only worked with SQL Server. Marcus (DBA) has basic PostgreSQL knowledge from a previous role but hasn't used it in 5 years.

10.6. `[Per app]` SMEs available for the EBA?
> - Wei Lin (senior dev) — app architecture, business logic, Crystal Reports
> - Aisha (dev) — front-end, merchant workflows
> - Marcus (DBA) — PaymentsDB, SQL Server, schema
> - Sarah (DevOps) — infrastructure, deployment, networking
> - David Tan (VP Merchant Services) — business requirements, merchant priorities (available for kickoff + final review only)

10.7. `[Per app]` Any known blockers or concerns about modernization?
> 1. Crystal Reports — no .NET Core/.NET 8+ version exists. Must be replaced entirely.
> 2. Shared PaymentsDB — cannot migrate DB independently from PayGate.
> 3. InProc session state — must move to distributed session (Redis/DynamoDB) for ECS.
> 4. Dual auth model — merchants keep forms auth (migrate to ASP.NET Core Identity), internal staff move to Windows Auth / AD integration. Need to support both auth schemes in one app. AD DCs are on-prem only, reachable via VPN.
> 5. Team capacity — only 2 developers, one shared with PayGate.
> 6. No container experience — training needed before EBA.


---

## 11. Modernization Expectations & Target State

11.1. `[Per app]` If you had to modernize this application without external help, how long do you estimate it would take and how many developers would be needed?
> Wei Lin estimates 6-9 months with 2 full-time developers. Main effort: Crystal Reports replacement (~2 months), EF6 to EF Core migration (~1 month), session state + auth migration (~1 month), testing + stabilization (~2 months). He's unsure about the DB migration timeline.

11.2. `[Per app]` Do you have a preferred target architecture or AWS services in mind?
> ECS Fargate (Linux containers) for the application. Aurora PostgreSQL for the database. ElastiCache Redis for session state and caching. S3 for file uploads and generated reports. CloudWatch for logging and monitoring. Priya wants to avoid EKS — "Fargate is simpler for a team with no container experience."

11.3. `[Per app]` What does success look like for this modernization?
> 1. MerchantHub running on Linux containers with zero Windows dependencies
> 2. No more Crystal Reports — modern PDF generation that's testable and maintainable
> 3. 99.9% availability with auto-scaling for month-end peaks
> 4. CI/CD pipeline deploying to ECS (no more RDP + PowerShell)
> 5. Foundation for merchant mobile app in 2027

11.4. `[Per app]` What are the current annual licensing costs for this application stack?
> ⚠️ _Voyager finance team to confirm exact breakdown. Estimated: Windows Server license ~$15K, SQL Server Enterprise (shared — MerchantHub's share ~30% of $120K = ~$36K), Crystal Reports runtime license ~$3K. Total MerchantHub share: ~$54K/year._


---

## 12. Database

12.1. `[Per instance]` Database engine and version:
> SQL Server 2019 Enterprise (15.0.4375.4, CU30). Shared instance with PayGate — single SQL Server hosting both PayGate and MerchantHub databases. MerchantHub has its own database (MerchantHubDB) on the same instance, plus read-only access to PayGateDB for transaction/settlement data.

12.2. `[Per instance]` Standalone or clustered? HA setup:
> Always On Availability Group — 2-node synchronous commit (primary + secondary in same datacenter). Automatic failover. Secondary is readable (used for reporting queries from ComplianceReporter).

12.3. `[Per instance]` Total database size (data + log):
> MerchantHubDB: 45 GB (data: 38 GB, log: 7 GB). PayGateDB (shared, read-only access): 850 GB total. Combined instance: 895 GB.

12.4. `[Per instance]` Workload type:
> MerchantHubDB: Mixed (OLTP for dispute management + reporting for merchant statements). PayGateDB access: read-only OLTP (transaction lookups, settlement queries).

12.5. `[Per project]` Data access layer:
> Entity Framework 6.4.4 (Database First). Two DbContext classes: MerchantHubContext (read-write to MerchantHubDB, ~40 entity classes) and PayGateReadContext (read-only to PayGateDB, ~15 entity classes mapping to PayGate tables). Some raw SQL queries in 3 repositories for complex reporting aggregations that EF couldn't express efficiently.

12.6. `[Per instance]` Database object counts:
> MerchantHubDB: 42 tables, 12 views, 35 stored procedures, 8 functions, 2 triggers. Most SPs are for report data aggregation. Triggers: audit trail on MH_Users and MH_Disputes tables.
> PayGateDB (MerchantHub accesses): reads ~15 tables, 5 views. Does not call PayGateDB stored procedures — all access is via EF LINQ queries.

12.7. `[Per instance]` SQL Server component services in use?
> MerchantHubDB: No SSIS, no SSRS, no SSAS. The SSRS entry in the Phase 100 inventory was incorrect for MerchantHub — SSRS is used by ComplianceReporter against PayGateDB, not by MerchantHub. MerchantHub uses Crystal Reports at the application layer, not SSRS.

12.8. `[Per instance]` Any of these features in use? (Yes/No for each)
- CLR assemblies: No
- Linked Servers: No
- SQL Agent Jobs: Yes — 2 jobs: (1) nightly index rebuild on MerchantHubDB (2am), (2) weekly statistics update (Sunday 3am)
- Replication (Transactional / Merge / CDC): No
- TDE or SQL Audit: Yes — TDE enabled on both PayGateDB and MerchantHubDB (PCI requirement). SQL Audit enabled for admin-level operations.
- Table Partitioning or Memory-Optimized Tables: No
- Spatial data types or Full-Text Search: No
- Database Mail / SQL Alerts: Yes — Database Mail configured for SQL Agent job failure notifications to Marcus.

12.9. `[Per instance]` Authentication — Windows Auth, SQL Auth, or both?
> Mixed Mode. MerchantHub application connects via SQL Authentication (dedicated service account: svc_merchanthub). Marcus wants to move to IAM database authentication on Aurora.

12.10. `[Per instance]` Backup strategy and retention?
> Full backup daily (2am), differential every 6 hours, transaction log every 15 minutes. Retention: 30 days on-prem, 90 days on S3 (existing backup archival via AWS Storage Gateway). No CDC or replication to other systems. ReconcEngine reads PayGateDB directly (not MerchantHubDB).

12.11. `[Per instance]` Approximate number of concurrent database connections from the application:
> MerchantHubDB: ~50 concurrent connections (EF connection pool, min 10 / max 100). PayGateDB: ~30 concurrent connections (read-only pool, min 5 / max 50). Total: ~80 connections from MerchantHub.

12.12. `[Per instance]` Is there a preferred cutover window or maintenance window?
> ⚠️ _Voyager to confirm. MerchantHubDB migration can potentially happen independently of PayGateDB since it's a separate database. PayGateDB migration requires coordination with PayGate and ComplianceReporter. Marcus suggests a Saturday night window (10pm-6am SGT) for MerchantHubDB cutover. PayGateDB cutover needs DMS CDC for near-zero downtime — no acceptable maintenance window for PayGate._

12.13. `[Per instance]` Please attach the AWS SCT assessment report if available:
> ⚠️ _Not yet available. Marcus will run DMS Schema Conversion against MerchantHubDB and PayGateDB before the architecture workshop. Estimated: 1 week to set up connectivity and run the assessment._
