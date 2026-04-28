# ReconcEngine — Questionnaire Responses (filled by Priya + Marcus)

Filled quickly over email, some answers are terse. Priya said "this is the cleanest app we have, it was rewritten 2 years ago by our best developer who has since left."

## 1. Application Overview
- App name: ReconcEngine
- Business owner: Priya (Head of Engineering)
- Description: Nightly batch reconciliation. Matches our transaction records against bank settlement files (Visa, Mastercard, DBS, OCBC). Flags discrepancies for ops team to investigate.
- Criticality: High — if recon doesn't run, we don't know if we got paid
- Users: 5 ops staff review the output reports. The engine itself runs unattended.
- Downtime: can tolerate up to 4 hours. Must complete before 6 AM SGT (ops team starts reviewing at 7 AM)
- Strategic importance: long-lived, no plans to replace
- Compliance: PCI DSS — transaction data must be encrypted at rest and in transit
- Docs: we have a README in the repo and some sequence diagrams from when it was rewritten

## 2. Architecture & Stack
- Project types: 1 Console App (main engine), 2 Windows Services (file watcher for settlement files, retry handler for failed matches)
- Architecture: simple — console app orchestrates, calls into shared libraries
- Language: C#
- .NET version: 4.8
- Solutions: 1 solution, 5 projects (console app, 2 services, 2 class libraries)
- LoC: ~35K total
- Preferred approach: port (it's clean code, no reason to rewrite)
- Tech debt: Low — was rewritten 2 years ago with good patterns
- Windows deps: Windows Services (obviously), Windows Event Log for error logging
- Session state: N/A (not a web app)
- Tight coupling: No — clean separation between engine, file watcher, and retry handler

## 3. Config, Caching, Storage
- Config: appsettings.json per environment (dev, uat, prod). Connection strings in there too (yes I know, not great)
- Local disk: yes — settlement files land on a shared folder (\\fileserver\settlements\), engine reads from there. Output reports written to \\fileserver\recon-reports\
- Caching: none
- Logging: Serilog to file + Windows Event Log

## 4. Dependencies
- NuGet: all public (Serilog, Dapper, CsvHelper, Polly)
- No third-party UI (it's a console app)
- Shared libraries: 2 internal class libs (ReconCore, ReconModels) — only used by ReconcEngine
- APIs exposed: none
- APIs consumed: none (reads files, writes to DB)
- Internal integrations: reads settlement files from SFTP drop folder (\\fileserver\settlements\), reads transaction data from PayGate DB (read-only), writes to Recon DB
- External integrations: none directly — settlement files come from banks via SFTP managed by infra team
- Downstream: ops team reads Recon DB via BackOffice app. Also generates CSV reports.
- Reporting: no Crystal Reports or SSRS. Just CSV output.

## 5. Batch Jobs
- File watcher service: monitors \\fileserver\settlements\ for new files, 24/7
- Main engine: triggered by Windows Task Scheduler at 11 PM SGT nightly
- Retry handler: runs every 30 mins, retries failed matches
- Typical run: 15-45 minutes depending on volume
- Longest run: 2 hours (month-end)
- Data volume: ~500K transactions/day, settlement files are 50-200 MB each
- Monitoring: email alerts on failure (sends to ops-alerts@voyager.com)
- Failure handling: retry handler picks up failed batches. If still failing after 3 retries, alerts ops.
- Dependencies: must run after settlement files arrive (usually by 10 PM)
- File shares: yes — \\fileserver\settlements\ (input), \\fileserver\recon-reports\ (output)

## 6. Auth & Security
- Auth: runs as a Windows Service account (svc_recon@voyager.local)
- DB auth: SQL Authentication (dedicated recon user)
- Encryption: TLS for DB connections. Settlement files are PGP encrypted, engine decrypts them.

## 7. Hosting
- Hosted on: dedicated Windows Server 2019 VM (on-prem)
- Specs: 4 vCPU, 16 GB RAM, 500 GB disk
- Traffic: N/A (batch)
- HA: none — single server. If it fails, ops runs it manually next morning.
- SLA: must complete by 6 AM SGT
- Monitoring: Nagios checks if the service is running
- Sandbox: yes — we have a dev environment with sample settlement files and a test DB

## 8. Testing
- Unit tests: yes, xUnit. ~60% coverage on ReconCore library
- Integration tests: yes, runs against test DB with sample data
- Load tests: no
- Test frequency: every commit via Jenkins

## 9. DevOps
- Source: GitHub
- CI/CD: Jenkins — builds on commit, deploys to dev automatically. UAT and prod are manual.
- Deployment: semi-automated (Jenkins builds, ops copies to server)
- Environments: Dev, UAT, Prod
- Deploy frequency: monthly
- Container experience: none

## 10. Team
- In-house
- Source code: yes, full access
- Developers: 1 (part-time, also works on PayGate)
- .NET proficiency: Advanced
- PostgreSQL experience: none
- SMEs for EBA: Priya (eng lead), Marcus (DBA), the developer
- Blockers: "the developer who rewrote it left. Current dev knows it but wasn't the original author"

## 11. Expectations
- Effort estimate: "maybe 2-3 months with 1 developer"
- Target: ECS Fargate, Aurora PostgreSQL
- Success: "ReconcEngine running on AWS, processing real settlement files, matching against Aurora PG"
- Licensing costs: ~$15K/year (Windows Server + SQL Express)

## 12. Database
- SQL Server 2019 Express, standalone
- 15 GB
- OLTP
- Dapper (micro-ORM, raw SQL)
- 12 stored procs — simple CRUD + a few reporting queries
- No SSIS/SSRS/SSAS
- No CLR, no linked servers, no replication
- SQL Agent: 1 job (nightly cleanup of old recon records)
- SQL Auth
- Backup: nightly full backup to file share
- ~10 concurrent connections
- Cutover window: weekend preferred, can tolerate a few hours of downtime