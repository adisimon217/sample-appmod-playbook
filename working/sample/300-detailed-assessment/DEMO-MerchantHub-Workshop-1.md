# MerchantHub Arch Workshop 1
Apr 8 2026, voyager office
Priya, Wei Lin, Aisha, Marcus, Sarah, AWS team

---

## auth (TD-001)

david wants AD for internal ops (~5 ppl). merchants keep their login obv

AWS pitched gMSA on fargate linux — kerberos ticket straight from AD, no middleman
priya: "never heard of gMSA on linux. is this real or experimental?"
AWS: its supported on fargate platform 1.4+ but yeah its newish
sarah: "vpn routing works, i tested from ec2. but havent tried from fargate"
wei lin: "and if it doesnt work?"
AWS: cognito w/ AD federation. works but adds a redirect, not transparent SSO
priya: "security team will hate cognito. but they also wont sign off on something unproven"

long back and forth. nobody comfortable deciding today

> need PoC. phase 400 thing. gMSA on fargate linux thru vpn to on-prem AD
> fallback = cognito
> NO DECISION YET

## shared DB situation (TD-002)

marcus drew on whiteboard (took photo)
- PayGateDB (850GB) and MerchantHubDB (45GB) are SEPARATE databases on same instance
- way better than we thought!! kickoff made it sound like shared tables
- MH has read-only EF access to ~15 paygate tables. no cross-db procs, no linked servers
- so we CAN migrate MerchantHubDB on its own
- dual connection: aurora pg (own stuff) + sql server via vpn (paygate reads)
- ef core supports multiple providers, npgsql + sqlclient in same app

marcus: "45 gig is nothing. 850 is the scary one and we dont touch it yet"
sarah: "direct connect routing confirmed, ec2 reaches on-prem sql fine"

> DECIDED: migrate MerchantHubDB independently. paygate stays on sql server til wave 2

## crystal reports (TD-003)

wei lin showed codebase — 12 .rpt files, SAP crystal 13.0.35, COM interop, GAC. dead on linux

looked at sample statement priya sent. 2 pages, logo + summary + txn table. nothing fancy

AWS suggested QuestPDF but priya: "we already pay for telerik (backoffice). telerik reporting runs on .net 6+ and linux"
wei lin checked it out — pdf/excel export, asp.net core viewer, visual designer
makes sense to use one vendor across portfolio

wei lin: "4 of 12 reports are dead btw. nobody downloads them. lemme confirm w david"

> DECIDED: telerik reporting replaces crystal. wei lin prototypes monthly statement before EBA. retire 4 dead reports pending david ok

## session + cache (TD-004)

inproc session = breaks w/ multiple containers obv
- redis vs dynamodb vs sticky sessions?
- priya: "sticky sessions feels like a hack"
- wei lin: session data is tiny, 2-5KB per merchant, ~4MB total at peak
- already need redis for caching (replace memorycache) so just use same cluster

AWS: "actually recommend valkey — wire compatible, no licensing headaches"
priya: "redis valkey whatever just make it work"

> DECIDED: elasticache valkey. sessions + cache, single cluster

## bank account encryption (TD-005)

marcus brought this up — merchant bank acct details stored in plain text. PCI concern

AWS: application-level encryption w/ AWS Encryption SDK + KMS
marcus: "thats a lot of code changes. every query touching bank details needs encrypt/decrypt"
wei lin: "how many places?... 3 repos, ~8 queries. manageable but not trivial"
priya: "is aurora's built-in encryption not enough?"
AWS: aurora encrypts at rest (KMS) but data is plaintext in memory + query results. PCI auditors may want field-level for financial data specifically
priya: "let me check w compliance team. dont want to over-engineer if aurora encryption satisfies them"

> NO DECISION. priya checking w compliance

## other stuff (not TDs, just decisions)

merchant auth: forms auth > asp.net core identity backed by aurora pg. bcrypt passwords stay. MFA = stretch goal. cognito eval deferred to post-EBA
> decided, straightfwd

url rewrite: sarah pulled up IIS — 4 rules. 3 are trivial (https redirect, mvc routing, trailing slash). rule 3 = legacy redirects, ~20 regex patterns, needs custom middleware
> decided

email: ~2000/day thru on-prem exchange. mock for EBA (IEmailService abstraction), SES post-EBA
> decided. priya starting SES domain verification

file uploads: dispute evidence on D:\, ~2GB. S3 w/ presigned URLs. trivial
> decided

hangfire: 3 jobs. wei lin wants statement gen in separate service (dont compete w web reqs). hangfire as separate ECS worker, eventbridge triggers nightly/daily, hourly cache stays as recurring job in worker
> decided

## actions
- [ ] AWS: scope gMSA PoC (TD-001) — phase 400
- [ ] wei lin: telerik reporting prototype (TD-003)
- [ ] wei lin: confirm dead reports w david (TD-003)
- [ ] priya: check w compliance on field-level encryption (TD-005)
- [ ] marcus: DMS schema conversion on MerchantHubDB (TD-002)
- [ ] sarah: valkey cluster in dev vpc (TD-004)
- [ ] sarah: check DC bandwidth for paygate read traffic
- [ ] priya: SES domain verification
- [ ] wei lin: IEmailService + mock