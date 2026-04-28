# MerchantHub Workshop 2 (follow-up)
April 15 2026, Teams call
Priya, Wei Lin, Marcus, Sarah, AWS

closing out action items from last week. shorter meeting

---

## TD-001 gMSA — still open

sarah: "tested from ec2, can reach AD DCs, kerberos works from windows ec2. havent tested from linux fargate tho"
AWS: "thats exactly what the PoC validates. 1-2 weeks, minimal test app"
priya: "and if it fails?"
AWS: "cognito w AD federation. proven but changes login UX"
priya: "ok do the PoC. if it fails we live with cognito"

> still YELLOW. PoC in phase 400

## TD-002 DMS results

marcus ran schema conversion on MerchantHubDB:
- 42 tables — 100%
- 12 views — 100%
- 35 SPs — 31 auto, 4 flagged (PIVOT + CROSS APPLY + windowing fns. "nothing scary")
- 8 functions — 7 auto, 1 flagged (STRING_AGG ordering, 5 line fix)
- 2 triggers — 100%

marcus: "95% automated. 2-4 hours manual review for the flagged stuff"

> GREEN. low risk. marcus reviews flagged items EBA week 1

## TD-003 telerik prototype

wei lin demoed side by side w crystal original. looks almost identical
- took ~6 hrs, used telerik report designer
- only diff = font (crystal used custom voyager font thats windows-only, switched to Inter)
- data layer unchanged, same SPs same result sets
- reports project went from 4K LoC to ~2K (template files handle layout)
- david confirmed 4 reports dead, approved retiring

priya: "looks great. ship it"

> GREEN. 8 reports to port (7 remaining + statement done). 4 retired

## TD-004 valkey — confirmed

sarah: "cluster up in dev vpc, single node, <1ms latency. multi-AZ in prod"
sarah: "DC is 500Mbps, current util ~80Mbps. MH paygate reads maybe 10-20Mbps extra. plenty of room"

> GREEN

## TD-005 encryption — still open

priya: "talked to compliance. aurora encryption at rest satisfies PCI 3.4. but they RECOMMEND app-level encryption for bank acct numbers as defense-in-depth. not mandatory tho. auditor flagged it as best practice last time"

priya: "i dont want to delay EBA for this"
AWS: "implement post-EBA as hardening. aurora KMS covers PCI requirement for now"
wei lin: "id rather do it right from the start. if we design the data access w encryption in mind now its easier than retrofitting"
priya: "ok compromise — wei lin designs the abstraction so encryption CAN be added. we decide during EBA week 1 whether to actually implement or defer"

> still YELLOW. wei lin designs encryption-ready abstraction. decision during EBA wk1

## EF6 → EF Core

wei lin walked thru data layer:
- 2 DbContexts, both database first w EDMX
- MerchantHubContext: 40 entities, r/w to MerchantHubDB
- PayGateReadContext: 15 entities, read-only to PayGateDB
- EDMX has to go (ef core doesnt support it). code first + fluent api
- entity classes already generated so mostly adding dbcontext config
- 3 repos w raw SQL:
  1. MonthlyStatementRepo — PIVOT query, goes away w telerik rewrite
  2. TransactionSearchRepo — dynamic WHERE builder (string concat, parameterized at least)
  3. DisputeReportRepo — GROUP BY w ROLLUP, sql server specific
- two-provider: npgsql for MerchantHubDB, sqlclient for PayGateDB

> ATX .NET handles bulk. wei lin does EDMX>fluent, 3 raw SQL repos, PIVOT>PG. ~2-3 days

## containers

- mcr.microsoft.com/dotnet/aspnet:10.0 linux
- 2 ECS services: web (web+api in one container) and worker (hangfire)
- ALB → web, worker has no public endpoint
- codepipeline: github → codebuild → ecr → ecs
- sarah hasnt done codepipeline before, will figure out during EBA wk2
- wei lin asked about splitting web and api into separate containers — not worth it for EBA

> single container web+api, separate container hangfire. CI/CD during EBA

## dev env

- WSL2 + docker desktop, .NET 10 SDK
- wei lin has used WSL, aisha hasnt
- priya: "do this BEFORE EBA. dont waste EBA time on env issues"

> both devs set up by april 25. validation = docker build + run hello-world .net 10 on linux

## timeline

priya: "whats the timeline look like?"
AWS: "2wk pre-EBA prep, gMSA PoC runs in parallel, EBA kickoff may 5, 6 weeks, done by mid june"
priya: "within johns 3 month target. lets do it"

> EBA kickoff May 5 2026. pre-EBA prep starts now. gMSA PoC must complete before EBA

## actions
- [ ] AWS: gMSA PoC plan (TD-001) — phase 400
- [ ] AWS: EBA plan from assessment + workshop decisions
- [ ] wei lin: port remaining 7 reports to telerik — EBA wk1
- [ ] wei lin + aisha: WSL2 + docker + .NET 10 — by apr 25
- [ ] wei lin: IEmailService + mock
- [ ] wei lin: encryption-ready data access abstraction (TD-005)
- [ ] marcus: review 4 flagged SPs + 1 function
- [ ] marcus: provision MerchantHubDB backup for dev/test
- [ ] sarah: aurora pg cluster in dev vpc
- [ ] sarah: ECR repo
- [ ] sarah: SES domain verification (dns records in, waiting propagation)
- [ ] priya: confirm EBA dates w john