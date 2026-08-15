# .NET & SQL Server Modernization — Pre-EBA Discovery Questionnaire

**Purpose:** Gather the data needed to assess modernization complexity, approach, scope, and plan for a 2-Day Experience-Based Acceleration (EBA).

**Target State:** .NET application containerized on Amazon ECS (Fargate/Linux) + SQL Server migrated to Amazon Aurora PostgreSQL.

**Instructions:** Please complete this questionnaire for each application to be modernized. Replace each `_Your answer here_` placeholder with your response. If a question is not applicable, write "N/A".

**Scope tags:** Each question is tagged to indicate whether the answer applies to the entire application, per project/component within the solution, or per database instance.
- `[Per app]` — answer once for the entire application
- `[Per project]` — answer for each project (.csproj) in the solution if they differ
- `[Per component]` — answer for each distinct component (e.g., each Windows Service, each API, each batch job)
- `[Per instance]` — answer for each database instance
- `[Per team]` — answer once for the team working on this application

---

## 1. Application Overview

1.1. `[Per app]` Application name:
> _Your answer here_

1.2. `[Per app]` Business owner / stakeholder:
> _Your answer here_

1.3. `[Per app]` Brief description — what does the application do?
> _Your answer here_

1.4. `[Per app]` Business criticality (Critical / High / Medium / Low):
> _Your answer here_

1.5. `[Per app]` Primary users — internal, external, or both? Estimated number of active users and their locations?
> _Your answer here_

1.6. `[Per app]` What happens during downtime? Maximum tolerable downtime?
> _Your answer here_

1.7. `[Per app]` What is the application's strategic importance and expected remaining lifetime? Any plans to replace or sunset it?
> _Your answer here_

1.8. `[Per app]` Any compliance, regulatory, or data-residency constraints?
> _Your answer here_

1.9. `[Per app]` Is there existing documentation we can review? (Architecture diagrams, runbooks, API specs — please share)
> _Your answer here_


---

## 2. Application Architecture & Technology Stack

2.1. `[Per project]` Any of these application/project types in the solution? (Yes/No for each)
- ASP.NET Web Forms:
- ASP.NET MVC:
- ASP.NET Web API:
- WCF (Windows Communication Foundation):
- WPF (Windows Presentation Foundation):
- XBAP (XAML Browser Application):
- Windows Workflow Foundation (WF):
- Windows Service:
- Console Application:
- Blazor / SPA (Angular, React):
- gRPC / Worker Service:
- Class Library:
> _Your answer here_

2.2. `[Per app]` Architecture style (Monolithic / SOA / Microservices / N-Tier):
> _Your answer here_

2.3. `[Per project]` Language(s) used (e.g., C#, VB.NET, F#):
> _Your answer here_

2.4. `[Per project]` .NET Framework / .NET version (e.g., .NET Framework 4.5.2, .NET Framework 4.8, .NET 6, .NET 8):
> _Your answer here_

2.5. `[Per app]` Number of solutions (.sln) and projects (.csproj / .vbproj). If multiple, briefly describe the purpose of each project:
> _Your answer here_

2.6. `[Per project]` Estimated lines of code — overall and per major project if known (see Appendix for tools):
> _Your answer here_

2.7. `[Per app]` What is the team's preferred modernization approach? (Port existing code to .NET 8+ / Rewrite/refactor from the ground up / Unsure — need guidance):
> _Your answer here_

2.8. `[Per app]` How would you rate the current tech debt level? (Low / Moderate / High / Critical). Is there a tech debt registry or backlog?
> _Your answer here_

2.9. `[Per component]` Any of these Windows-OS or IIS-specific dependencies in use? (Yes/No for each)
- COM / COM+ / Interop components:
- OLE / Office Interop (e.g., Excel/Word automation):
- P/Invoke calls to native DLLs:
- Windows Registry:
- Global Assembly Cache (GAC):
- MSMQ / Windows Event Log:
- IIS URL Rewrite rules:
- Custom IIS modules or ISAPI filters:
- IIS virtual directories or sub-applications:
- Windows Authentication via IIS (Negotiate/NTLM):
- web.config handlers, machine keys, or httpModules:
> _Your answer here_

2.10. `[Per app]` Does the application store session state or use cookies? Where is state stored?
> _Your answer here_

2.11. `[Per app]` Does the application have tightly coupled dependencies between modules?
> _Your answer here_


---

## 3. Configuration, Caching & Storage

3.1. `[Per app]` How is application configuration managed? (web.config transforms, environment variables, config files per environment, external config server). Where are database connection strings stored and how are they managed across environments?
> _Your answer here_

3.2. `[Per component]` Does the application write to local disk? (e.g., file uploads, temp files, generated reports, logs). If yes, what paths and approximate volume?
> _Your answer here_

3.3. `[Per app]` Any caching in use? (e.g., in-memory MemoryCache, Redis, AppFabric/Velocity, NCache):
> _Your answer here_

3.4. `[Per app]` What logging framework is in use? (e.g., log4net, NLog, Serilog, System.Diagnostics, ETW):
> _Your answer here_


---

## 4. Dependencies & Integrations

4.1. `[Per project]` NuGet packages — public or private feed? (Please attach packages.config or .csproj snippet if possible)
> _Your answer here_

4.2. `[Per project]` Any paid or third-party UI/component libraries in use? (e.g. Telerik, DevExpress, Infragistics, SyncFusion, etc.)
If "Yes" please include: (a) product name and version, (b) which project(s) use it, (c) does your current license include .NET / ASP.NET Core editions, (d) is the team willing to repurchase the modern .NET edition if needed?
> _Your answer here_

4.3. `[Per app]` Any internal / shared libraries? Names and purpose:
> _Your answer here_

4.4. `[Per component]` Does the application expose APIs? What technology? (REST, SOAP/WCF, gRPC):
> _Your answer here_

4.5. `[Per component]` Does the application consume external APIs or services? Please list:
> _Your answer here_

4.6. `[Per component]` Internal integration points (e.g., message queues, file shares, FTP, ETL):
> _Your answer here_

4.7. `[Per component]` External integration points (e.g., partner APIs, SaaS, payment gateways):
> _Your answer here_

4.8. `[Per component]` Any downstream applications dependent on this application? Sync or async communication?
> _Your answer here_

4.9. `[Per app]` Does the application use Crystal Reports / SSRS / other reporting tools?
> _Your answer here_


---

## 5. Batch Jobs & Background Processing

5.1. `[Per component]` Are there Windows Services or console-based batch applications? If yes, what is the purpose of each?
> _Your answer here_

5.2. `[Per component]` Are they scheduled (how often?) or running 24/7? What scheduling mechanism is used? (e.g., Windows Task Scheduler, SQL Agent, Hangfire, Quartz.NET, custom):
> _Your answer here_

5.3. `[Per component]` How long does a typical batch run last? (e.g., seconds, minutes, hours). What is the longest observed run time?
> _Your answer here_

5.4. `[Per component]` What data volumes does each batch job process per run? (e.g., number of records, file sizes):
> _Your answer here_

5.5. `[Per component]` How are batch jobs monitored? Is there alerting on failures or long-running jobs? (e.g., email alerts, dashboard, log files, SCOM):
> _Your answer here_

5.6. `[Per component]` What happens when a batch job fails? Is there an automatic retry mechanism? How are partial failures handled — does the job resume from where it left off or restart from scratch?
> _Your answer here_

5.7. `[Per component]` Dependencies on other jobs or external triggers:
> _Your answer here_

5.8. `[Per component]` Do any batch jobs monitor or read/write to Windows file shares (UNC paths, SMB/CIFS)? If yes, which shares and what file patterns?
> _Your answer here_


---

## 6. Authentication & Security

6.1. `[Per app]` Authentication method (e.g., Active Directory, OIDC, OAuth 2.0, ASP.NET Identity with SQL, SAML):
> _Your answer here_

6.2. `[Per app]` Authorization model (role-based, claims-based, custom):
> _Your answer here_

6.3. `[Per app]` Any encryption requirements beyond TLS? (at rest, field-level, etc.)
> _Your answer here_


---

## 7. Hosting, Scaling & Resilience

7.1. `[Per app]` How is the application currently hosted? (e.g., on-prem VMs, IIS on bare metal, Azure App Service). Is the application containerized today? If yes, what runtime? (Docker, Kubernetes, OpenShift, ECS, other):
> _Your answer here_

7.2. `[Per instance]` Hardware specs per instance (vCPUs, RAM, disk):
> _Your answer here_

7.3. `[Per app]` Traffic pattern (Steady state / Known peaks / Unpredictably spiky):
> _Your answer here_

7.4. `[Per app]` Is the application highly available today? What HA / DR mechanism?
> _Your answer here_

7.5. `[Per app]` SLA, RPO, and RTO targets:
> _Your answer here_

7.6. `[Per app]` Monitoring and alerting tools in use (e.g., SCOM, Datadog, AppDynamics):
> _Your answer here_

7.7. `[Per app]` Can the full application stack (app + database + dependencies) be loaded in an isolated sandbox environment for modernization work? (e.g., local laptop, shared dev VM, cloud dev environment). If any dependencies cannot be sandboxed, are mock interfaces or stubs available?
> _Your answer here_


---

## 8. Testing & Code Quality

8.1. `[Per project]` Are there unit or integration tests in place? What framework? (MSTest, NUnit, xUnit):
> _Your answer here_

8.2. `[Per project]` Approximate unit test code coverage percentage:
> _Your answer here_

8.3. `[Per app]` Are there load, performance, security, or end-to-end tests in place? Tools/frameworks used?
> _Your answer here_

8.4. `[Per app]` How often are tests run? (Every commit / nightly / manual):
> _Your answer here_


---

## 9. DevOps & Environments

9.1. `[Per app]` Source control system (e.g., Git/GitHub, Azure DevOps/TFS, Bitbucket):
> _Your answer here_

9.2. `[Per app]` CI/CD pipeline in place? What tool? (e.g., Azure DevOps, Jenkins, GitHub Actions):
> _Your answer here_

9.3. `[Per app]` Deployment process (Manual / Semi-automated / Fully automated):
> _Your answer here_

9.4. `[Per app]` Environments in use (e.g., Dev, Test, QA, UAT, Staging, Prod). Are they provisioned identically?
> _Your answer here_

9.5. `[Per app]` How often is new code deployed to production? (e.g., daily, weekly, monthly, quarterly):
> _Your answer here_

9.6. `[Per team]` Any existing container or Linux experience on the team? (Docker, OpenShift, Kubernetes):
> _Your answer here_


---

## 10. Team & Readiness

10.1. `[Per app]` Application developed/maintained in-house or by a vendor?
> _Your answer here_

10.2. `[Per app]` Is the full source code available and accessible?
> _Your answer here_

10.3. `[Per app]` Number of developers actively working on this application:
> _Your answer here_

10.4. `[Per team]` Team's .NET proficiency level (Beginner / Intermediate / Advanced):
> _Your answer here_

10.5. `[Per team]` Team's experience with PostgreSQL:
> _Your answer here_

10.6. `[Per app]` SMEs available for the EBA? (Names & roles — we need app dev, DBA, and infra/DevOps):
> _Your answer here_

10.7. `[Per app]` Any known blockers or concerns about modernization?
> _Your answer here_


---

## 11. Modernization Expectations & Target State

11.1. `[Per app]` If you had to modernize this application without external help, how long do you estimate it would take and how many developers would be needed?
> _Your answer here_

11.2. `[Per app]` Do you have a preferred target architecture or AWS services in mind? (e.g., ECS/Fargate, EKS, Lambda, EC2 Linux, Aurora PostgreSQL, RDS):
> _Your answer here_

11.3. `[Per app]` What does success look like for this modernization? What outcomes matter most to your team?
> _Your answer here_

11.4. `[Per app]` What are the current annual licensing costs for this application stack? (e.g., Windows Server, SQL Server, IIS, third-party components):
> _Your answer here_


---

## 12. Database

12.1. `[Per instance]` Database engine and version (e.g., SQL Server 2019 Enterprise, PostgreSQL 15, MySQL 8, file-based, none). If not SQL Server, answer the remaining questions in this section where applicable or write "N/A":
> _Your answer here_

12.2. `[Per instance]` Standalone or clustered? HA setup (Always On AG, FCI, Log Shipping, none)?
> _Your answer here_

12.3. `[Per instance]` Total database size (data + log):
> _Your answer here_

12.4. `[Per instance]` Workload type (OLTP / Data Warehouse / Reporting / Mixed):
> _Your answer here_

12.5. `[Per project]` Data access layer (e.g., ADO.NET, Entity Framework 6, Dapper, NHibernate, raw SQL):
> _Your answer here_

12.6. `[Per instance]` Database object counts — Tables, Views, Stored Procedures, Functions, Triggers (approximate is fine). Are there complex business rules/logic in these objects?
> _Your answer here_

12.7. `[Per instance]` SQL Server component services in use? (SSIS, SSRS, SSAS — briefly describe):
> _Your answer here_

12.8. `[Per instance]` Any of these features in use? (Yes/No for each)
- CLR assemblies:
- Linked Servers:
- SQL Agent Jobs:
- Replication (Transactional / Merge / CDC):
- TDE or SQL Audit:
- Table Partitioning or Memory-Optimized Tables:
- Spatial data types or Full-Text Search:
- Database Mail / SQL Alerts:
> _Your answer here_

12.9. `[Per instance]` Authentication — Windows Auth, SQL Auth, or both?
> _Your answer here_

12.10. `[Per instance]` Backup strategy and retention? Any database synchronization with other systems? (ETL, CDC, replication):
> _Your answer here_

12.11. `[Per instance]` Approximate number of concurrent database connections from the application:
> _Your answer here_

12.12. `[Per instance]` Is there a preferred cutover window or maintenance window for the database migration? Any data freshness requirements (e.g., maximum acceptable data lag during migration)?
> _Your answer here_

12.13. `[Per instance]` Please attach the AWS SCT assessment report if available (see Appendix for instructions):
> _Attached / Not yet available_


---

## Appendix — Helpful Tools & References

The following tools can help you gather accurate answers for this questionnaire. All are optional but recommended.

**.NET Assessment & Modernization (for sections 2, 7)**
- [AWS Transform for .NET](https://docs.aws.amazon.com/transform/latest/userguide/dotnet.html) — AI-powered service that analyzes .NET Framework codebases, generates transformation plans, and automates code conversion to cross-platform .NET. Available via the AWS Console or the [AWS Toolkit for Visual Studio](https://docs.aws.amazon.com/transform/latest/userguide/dotnet-ide.html). Supports .NET Framework 3.5+, C# class libraries, console apps, ASP.NET (MVC/Web API/Web Forms), WCF services, and unit test projects. The analysis report helps answer questions about LoC, project structure, and compatibility.
- [Kiro](https://kiro.dev/docs/) — AI-powered IDE that can assist with code analysis, porting, and transformation tasks not covered by the AWS Transform agents.

**Lines of Code (for question 2.6)**
- [cloc](https://github.com/AlDanial/cloc) — open-source LoC counter. Run: `cloc --by-file --include-lang=C#,VB.NET,XML,JSON,SQL .`

**Code Quality & Complexity (for section 7)**
- [SonarQube](https://www.sonarsource.com/products/sonarqube/) — LoC, code coverage, code smells, duplication, complexity

**NuGet Package Inventory (for question 4.1)**
- Run `dotnet list MySolution.sln package` to export all NuGet dependencies. For older .NET Framework projects using `packages.config`, the package list is already in that file.

**SQL Server Enterprise Feature Usage (for question 12.8)**
- Follow the T-SQL script in the AWS guide: [Assessing your environment — Evaluate downgrading SQL Server Enterprise to Standard](https://docs.aws.amazon.com/prescriptive-guidance/latest/evaluate-downgrading-sql-server-edition/assess-environment.html)

**SQL Server Modernization & Database Migration (for questions 12.x)**
- [AWS Transform for SQL Server Modernization](https://docs.aws.amazon.com/transform/latest/userguide/sql-server-modernization.html) — AI-powered service that automates full-stack modernization of SQL Server databases and associated .NET applications to Amazon Aurora PostgreSQL. Handles schema conversion, stored procedure transformation (T-SQL → PL/pgSQL), data migration, and application code updates (Entity Framework, ADO.NET, connection strings). Supports SQL Server 2008 R2 through 2022.
- [AWS Schema Conversion Tool (SCT)](https://docs.aws.amazon.com/SchemaConversionTool/latest/userguide/CHAP_Installing.html) — connect to your SQL Server and generate an assessment report. It includes object counts, total lines of code, conversion complexity, and action items. Save as PDF or CSV. See: [Viewing the assessment report](https://docs.aws.amazon.com/SchemaConversionTool/latest/userguide/CHAP_UserInterface.AssessmentReport.html).
- Alternatively, use [DMS Schema Conversion](https://docs.aws.amazon.com/dms/latest/sbs/schema-conversion-sql-server-aurora-postgresql.html) (serverless, no install required).
- The [AWS DMS diagnostic support script for SQL Server](https://docs.aws.amazon.com/dms/latest/userguide/CHAP_SupportScripts.SQLServer.html) collects database configuration into an HTML report.

**Further Reading**
- [SQL Server to Aurora PostgreSQL Migration Playbook](https://docs.aws.amazon.com/dms/latest/sql-server-to-aurora-postgresql-migration-playbook/Welcome.html)
- [Modernize .NET workloads on AWS](https://aws.amazon.com/developer/language/net/modernize/)
- [Refactor .NET to Linux — Cost Optimization](https://docs.aws.amazon.com/prescriptive-guidance/latest/optimize-costs-microsoft-workloads/net-refactor-linux.html)
- [AWS .NET Developer Blog](https://aws.amazon.com/blogs/developer/category/programing-language/dot-net/)
