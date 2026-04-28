# Detailed Analysis — From Assessment to EBA Execution

**Purpose:** This document bridges the [Pre-EBA Discovery Questionnaire](300-App-Questionnaire.md) and the [EBA Preparation Checklist](EBA-Preparation-Checklist.md). For each questionnaire response, it explains how the AWS team should interpret the answer and what execution decisions it drives during the 6-week engagement and 2-day EBA Party.

**Execution Methodologies:**
- [AWS AI-DLC (AI-Driven Development Life Cycle)](https://github.com/awslabs/aidlc-workflows) — for AI-assisted code transformation and development
- [ModAx EBA (Modernization Accelerator: Experience-Based Acceleration)](https://aws.amazon.com/experience-based-acceleration/) — for the intensive 2-day hackathon delivery

**Target State:** .NET on Amazon ECS Fargate (Linux) + Amazon Aurora PostgreSQL. Target the latest .NET LTS version (currently .NET 10).

**Plan Decisiveness:** When generating modernization plans from this playbook, be decisive — recommend one approach per decision point, not a menu of options. At most present 2 alternatives only when the choice genuinely depends on customer input that hasn't been gathered yet. The EBA plan should read as a clear execution blueprint, not a consulting options paper.

---

## Section 1 — Application Overview → Scoping & Risk Assessment

| Q# | Question | Scope | How It Drives EBA |
|----|----------|-------|-------------------|
| 1.1 | Application name | Per app | Identifies the workload. Used as the label across all EBA artefacts. |
| 1.2 | Business owner / stakeholder | Per app | Determines who approves scope, attends the EBA Party, and signs off on the demo. |
| 1.3 | Brief description | Per app | Frames the modernization narrative. Helps the AWS team understand domain context before touching code. |
| 1.4 | Business criticality | Per app | **Critical/High** → stricter rollback plan, more testing during EBA. **Medium/Low** → more room for aggressive refactoring. |
| 1.5 | Primary users & locations | Per app | Drives AWS region selection, latency requirements, and whether multi-region is needed post-EBA. |
| 1.6 | Downtime tolerance | Per app | Determines cutover strategy. **Zero tolerance** → blue/green deployment on ECS. **Hours tolerable** → simpler rolling deployment. |
| 1.7 | Strategic importance / lifetime | Per app | **Sunset planned** → don't invest in deep modernization, consider lift-and-shift. **Long-lived** → invest in proper refactoring. |
| 1.8 | Compliance / data residency | Per app | Constrains region selection, encryption requirements, and whether data can leave on-prem during EBA. May require private subnets, VPC endpoints, or PrivateLink. |
| 1.9 | Existing documentation | Per app | Available docs accelerate EBA prep. **No docs** → allocate more time for architecture discovery in weeks 1-2. |


---

## Section 2 — Application Architecture & Technology Stack → Modernization Approach Selection

| Q# | Question | Scope | How It Drives EBA |
|----|----------|-------|-------------------|
| 2.1 | Application/project types (Yes/No checklist) | Per project in solution | **This is the single most important question for execution planning.** See decision matrix below. |
| 2.2 | Architecture style | Per app | **Monolithic** → likely single container, may need to extract config. **N-Tier** → map tiers to separate ECS services. **Microservices** → already container-friendly, focus on orchestration. |
| 2.3 | Language(s) | Per project | **C#** → full AWS Transform support. **VB.NET** → AWS Transform supports it but conversion to C# may be recommended. **F#** → manual porting with Kiro. |
| 2.4 | .NET Framework version | Per project | **≥ .NET Framework 3.5** → AWS Transform for .NET supported. **< 3.5** → AWS Transform not supported, but still viable for EBA if the app is small/simple enough — use Kiro + AI-DLC to rewrite. **.NET 6+** → already cross-platform, focus on containerization only. |
| 2.5 | Solutions & projects breakdown | Per app | Determines EBA work breakdown structure. Each project becomes a work item. Large solutions (>20 projects) may need to be split across multiple EBA sprints. |
| 2.6 | Lines of code | Per project | **< 50K LoC** → realistic for 2-day EBA. **50K–100K** → achievable but focus on critical path, defer non-essential projects. **> 100K** → likely beyond EBA scope — this is a full modernization project, not a 6-week sprint. Scope the EBA to a vertical slice or a single high-value component, with the remainder planned as a follow-on engagement. |
| 2.7 | Preferred approach (port/rewrite) | Per app | **Port** → AWS Transform for .NET + Kiro for cleanup. **Rewrite** → Kiro + AI-DLC workflows. **Unsure** → AWS team recommends based on 2.1/2.4/2.8 answers. |
| 2.8 | Tech debt level | Per app | **Low/Moderate** → port is viable. **High/Critical** → consider rewrite for core modules, port for utilities. Tech debt registry helps prioritize what to fix during EBA. |
| 2.9 | Windows/IIS dependencies (Yes/No checklist) | Per component | Each "Yes" is a blocker that needs a mitigation plan. See dependency resolution matrix below. |
| 2.10 | Session state / cookies | Per web project | **In-process session** → must externalize to Redis/ElastiCache or DynamoDB for container scaling. **SQL-based** → simpler, just update connection string. **Cookies only** → no change needed. |
| 2.11 | Tight coupling between modules | Per app | **Yes** → harder to containerize independently, may need single container initially. **No** → can split into multiple ECS services. |

### Decision Matrix: Project Type → Modernization Approach

| Project Type | Approach | Primary Tool | EBA Complexity | Notes |
|-------------|----------|-------------|----------------|-------|
| ASP.NET MVC | Port to ASP.NET Core MVC | AWS Transform for .NET | Low–Medium | Most straightforward path. Razor views largely compatible. |
| ASP.NET Web API | Port to ASP.NET Core Web API | AWS Transform for .NET | Low | Controllers and routing map cleanly. |
| ASP.NET Web Forms | Port to ASP.NET Core MVC or rewrite | AWS Transform Custom (port to MVC) or Kiro + AI-DLC (rewrite) | High | AWS Transform for .NET converts Web Forms to Blazor — use AWS Transform Custom instead to target MVC directly. If Transform Custom output needs heavy rework, fall back to Kiro rewrite. |
| WCF (SOAP services) | Port to CoreWCF or rewrite to gRPC/REST | AWS Transform for .NET (CoreWCF) or Kiro | Medium | AWS Transform supports WCF → CoreWCF. If consumers can change, prefer gRPC/REST. |
| WinForms | Build new — ASP.NET Core MVC or SPA (React) + Web API | Kiro + AI-DLC | High | WinForms is a Windows desktop technology with no cross-platform path. Not suitable for EBA — this is a full build-new project. Scope separately from the EBA engagement. |
| WPF | Build new — ASP.NET Core MVC or SPA (React) + Web API | Kiro + AI-DLC | High | WPF cannot run on Linux. Full UI rewrite required. Not suitable for EBA — scope as a separate build-new initiative. |
| XBAP | Rewrite to ASP.NET Core MVC or SPA (React) + Web API | Kiro + AI-DLC | High | Dead technology. Full rewrite. Likely out of EBA scope. |
| Windows Workflow Foundation (WF) | Rewrite to AWS Step Functions or custom orchestration | Kiro + AI-DLC | High | No .NET Core equivalent. Map workflows to Step Functions or Elsa Workflows. |
| Windows Service | Port to .NET Worker Service | AWS Transform for .NET + Kiro | Low–Medium | Worker Service runs on Linux. Deploy as ECS task or sidecar. |
| Console Application | Port to .NET LTS Console | AWS Transform for .NET | Low | Usually straightforward. Deploy as ECS scheduled task or Lambda. |
| Blazor / SPA | Already cross-platform | Kiro (if needed) | Low | Minimal changes. Focus on containerizing the hosting layer. |
| gRPC / Worker Service | Already cross-platform | Kiro (if needed) | Low | Minimal changes. |
| Class Library | Port to .NET LTS | AWS Transform for .NET | Low | Dependencies may need updating. Usually follows the main project. |
| Any project with third-party UI controls (Telerik, DevExpress, Infragistics, etc.) | Repurchase modern edition + rewrite UI, or full rewrite with standard controls | Kiro + AI-DLC (AWS Transform cannot port third-party components) | High | See Third-Party UI Component Decision Matrix in Section 4. License procurement or functional spec required in weeks 1–2. |

### Dependency Resolution Matrix (Question 2.9)

| Dependency | Impact | Mitigation for Linux Containers |
|-----------|--------|-------------------------------|
| COM / COM+ / Interop | Blocker | Rewrite as managed .NET code or wrap in a Windows sidecar service called via HTTP/gRPC. |
| OLE / Office Interop | Blocker | Replace with headless libraries: ClosedXML (Excel), DocX (Word), or AWS Lambda + LibreOffice layer. |
| P/Invoke to native DLLs | Blocker | Find Linux-compatible native libs or rewrite in managed code. |
| Windows Registry | Blocker | Externalize to environment variables, AWS Systems Manager Parameter Store, or Secrets Manager. |
| GAC | Medium | Move assemblies to NuGet packages or project-local references. |
| MSMQ | Blocker | Replace with Amazon SQS or SNS. |
| Windows Event Log | Medium | Replace with structured logging (Serilog → CloudWatch). |
| IIS URL Rewrite | Medium | Convert to ASP.NET Core middleware or YARP reverse proxy rules. |
| Custom IIS modules / ISAPI | Blocker | Rewrite as ASP.NET Core middleware. |
| IIS virtual directories | Medium | Map to ASP.NET Core path-based routing or separate ECS services. |
| Windows Auth via IIS | High | Replace with OIDC/OAuth 2.0 (Amazon Cognito or customer's IdP). If AD is required, use LDAP over Linux or AWS Managed Microsoft AD. |
| web.config handlers / machine keys | Medium | Migrate to ASP.NET Core configuration + AWS Secrets Manager for keys. |


---

## Section 3 — Configuration, Caching & Storage → Containerization Readiness

| Q# | Question | Scope | How It Drives EBA |
|----|----------|-------|-------------------|
| 3.1 | Configuration management & connection strings | Per app | **web.config transforms** → must externalize to environment variables or AWS Systems Manager Parameter Store for containers. **Connection strings in code/config** → must move to Secrets Manager. This is Day 1 EBA work — containerized apps cannot use file-based config transforms. |
| 3.2 | Local disk writes | Per component | **Yes** → container filesystems are ephemeral. Must redirect to Amazon S3 (uploads/reports), EFS (shared files), or CloudWatch Logs (logs). This is a blocker if not addressed before EBA. |
| 3.3 | Caching | Per app | **In-memory MemoryCache** → works in single container but breaks horizontal scaling. Must move to Amazon ElastiCache (Redis). **AppFabric/Velocity** → deprecated, replace with ElastiCache. **Redis already** → point to ElastiCache, minimal change. |
| 3.4 | Logging framework | Per app | **Serilog** → easiest path, add CloudWatch sink. **log4net/NLog** → add CloudWatch appender or switch to Serilog during EBA. **ETW/System.Diagnostics** → Windows-only, must replace. **Stretch goal:** add OpenTelemetry SDK for distributed tracing. |

---

## Section 4 — Dependencies & Integrations → Integration Risk & Scope Boundaries

| Q# | Question | Scope | How It Drives EBA |
|----|----------|-------|-------------------|
| 4.1 | NuGet packages (public/private) | Per project | **Private feed** → must configure access from AWS (CodeArtifact or VPN to on-prem feed). **Public only** → no issue. Package list feeds into AWS Transform compatibility analysis. |
| 4.2 | Paid UI/component libraries | Per project | **Any third-party UI library (Telerik, DevExpress, Infragistics, etc.) is a critical scoping factor.** AWS Transform for .NET cannot port these components or their proprietary NuGet packages. See Third-Party UI Component Decision Matrix below. |
| 4.3 | Internal / shared libraries | Per app | Shared libraries used by multiple apps may need to be ported first or maintained as dual-target (.NET Framework + .NET LTS). Identify if they're in the same repo or separate. |
| 4.4 | APIs exposed | Per component | **SOAP/WCF** → plan CoreWCF port or REST rewrite. **REST** → minimal change, update hosting. **gRPC** → already cross-platform. Exposed APIs define the contract that must not break during EBA. **⚠️ Legacy ASMX/SOAP endpoints require extra caution** — if other applications consume these endpoints, you cannot simply rewrite to REST. You must either maintain backward compatibility (e.g., SoapCore adapter) or coordinate consumer migration. Multiple downstream SOAP consumers significantly increase EBA risk and scope. Treat ASMX with downstream dependents as an architecture discussion point — see 300-Architecture.md. |
| 4.5 | External APIs consumed | Per component | External APIs are outside EBA scope but must be reachable from the sandbox environment. If not, mock interfaces needed (see 7.7). |
| 4.6 | Internal integration points | Per component | **MSMQ** → replace with SQS. **File shares** → replace with S3 or EFS. **FTP** → replace with AWS Transfer Family. Each integration point is a work item in the EBA backlog. |
| 4.7 | External integration points | Per component | External integrations define the EBA scope boundary. Anything outside the boundary needs a stub or pass-through during the pilot. |
| 4.8 | Downstream dependents | Per component | Downstream apps that depend on this app's APIs must not break. EBA must maintain backward-compatible interfaces or run old and new in parallel. |
| 4.9 | Reporting tools | Per app | **Crystal Reports** → no Linux support, must replace with alternative (e.g., SSRS → Paginated Reports, or move to Amazon QuickSight). **SSRS** → if tightly coupled, may need to remain on SQL Server initially. |

### Third-Party UI Component Decision Matrix (Question 4.2)

AWS Transform for .NET cannot port third-party UI controls (Telerik, DevExpress, Infragistics, ComponentOne, Syncfusion, etc.) or their proprietary NuGet packages. When the questionnaire reveals third-party UI components, use this matrix to determine the modernization path. The primary tool for both paths is Kiro + AI-DLC — AWS Transform is not an option here.

| Scenario | Approach | Tool | EBA Impact | Pre-requisites |
|----------|----------|------|------------|----------------|
| Vendor offers .NET Core / ASP.NET Core edition AND customer is willing to repurchase | **Repurchase + Rewrite UI layer** using vendor's modern controls | Kiro + AI-DLC | High — new control APIs differ significantly from legacy; UI will look different | License procurement must complete before EBA (weeks 1–2). Customer must accept that UI will differ from legacy. |
| Vendor offers modern edition BUT customer is unwilling to repurchase | **Full rewrite** replacing vendor controls with standard ASP.NET Core MVC (Razor/Tag Helpers) or SPA (React) + Web API | Kiro + AI-DLC | High — all vendor-dependent UI pages must be rewritten from scratch | Functional spec with user stories for affected pages (weeks 1–2). Customer must accept UI will look different. |
| Vendor does NOT offer a .NET Core / ASP.NET Core edition | **Full rewrite** replacing vendor controls with standard ASP.NET Core MVC or SPA (React) + Web API | Kiro + AI-DLC | High — no migration path exists, full rewrite is the only option | Functional spec with user stories for affected pages (weeks 1–2). Customer must accept UI will look different. |
| Third-party components are in non-UI layers (e.g., PDF generation, charting libraries) | **Replace with .NET Core-compatible alternatives** (e.g., QuestPDF, SkiaSharp, ClosedXML) | Kiro + AI-DLC | Medium — API surface is typically smaller than UI controls | Identify replacement libraries in weeks 1–2. |

#### Key Principles

- **AWS Transform is off the table** for any project that uses third-party UI components. The entire UI layer for those projects must go through Kiro + AI-DLC.
- **The UI will look different.** This is non-negotiable and must be communicated to the customer upfront. Legacy Telerik/DevExpress Web Forms controls have no visual equivalent in modern ASP.NET Core.
- **Scope carefully for EBA.** If the application is heavily dependent on third-party controls (e.g., complex grids, schedulers, editors on every page), the 2-day EBA should target a vertical slice — a few representative pages — not the full UI.
- **Functional spec is mandatory** for the rewrite path. Without it, the EBA team will waste time reverse-engineering legacy control behavior. Develop the spec in weeks 1–2 using Kiro + AI-DLC by analyzing the legacy code.
- **License procurement is a blocker** for the repurchase path. If the customer chooses to repurchase, the modern license must be in hand before the EBA Party. This cannot be resolved during the 2-day.

#### Impact on EBA Pilot Selection

Applications with heavy third-party UI dependencies score lower as EBA pilot candidates because:
- AWS Transform cannot be used, removing the fastest porting path
- UI rewrite effort is significant and hard to complete in 2 days
- Customer expectation management is needed (UI will look different)

If the best pilot candidate has third-party UI components, scope the EBA to backend + database modernization and a representative UI slice, with the remaining UI rewrite planned as a follow-on sprint.

---

## Section 5 — Batch Jobs & Background Processing → Worker Service Design

| Q# | Question | Scope | How It Drives EBA |
|----|----------|-------|-------------------|
| 5.1 | Windows Services / batch apps | Per component | Each Windows Service → port to .NET Worker Service, deploy as separate ECS task. Console apps → ECS scheduled tasks or EventBridge + Lambda. |
| 5.2 | Schedule / 24x7 & scheduling mechanism | Per component | **Scheduled** → ECS Scheduled Tasks or EventBridge rules. **24/7** → long-running ECS service with health checks. **SQL Agent** → migrate to EventBridge + Lambda or pg_cron on Aurora. **Hangfire/Quartz.NET** → port to .NET LTS, run in ECS. |
| 5.3 | Batch run duration | Per component | **Seconds/minutes** → suitable for Lambda or short-lived ECS tasks. **Hours** → must be long-running ECS task with health checks and timeout configuration. Longest observed run time sets the upper bound for ECS task timeout. |
| 5.4 | Data volumes per run | Per component | Drives ECS task memory/CPU sizing and storage requirements. **Large volumes** → may need EFS mount or S3 staging. Also affects whether Lambda (15-min limit, 10 GB /tmp) is viable. |
| 5.5 | Batch monitoring & alerting | Per component | **SCOM/email alerts** → replace with CloudWatch Alarms + SNS. **Dashboard** → CloudWatch Dashboards or existing tool with AWS integration. **No monitoring** → opportunity to add CloudWatch-based alerting during EBA (stretch goal). |
| 5.6 | Failure handling & retry | Per component | **Automatic retry** → implement equivalent in ECS (retry policies) or Step Functions (built-in retry/catch). **Resume from checkpoint** → must preserve checkpoint state externally (DynamoDB or S3). **Restart from scratch** → simpler, just re-run the ECS task. |
| 5.7 | Job dependencies / triggers | Per component | Complex job chains → consider AWS Step Functions for orchestration. Simple triggers → EventBridge rules. |
| 5.8 | Windows file share access | Per component | **Yes** → critical blocker for Linux containers. Options: (1) Amazon FSx for Windows File Server with SMB, (2) replace with S3 + event triggers, (3) Amazon EFS if POSIX-compatible. Must be resolved before EBA. |

---

## Section 6 — Authentication & Security → Identity Architecture

| Q# | Question | Scope | How It Drives EBA |
|----|----------|-------|-------------------|
| 6.1 | Authentication method | Per app | **Active Directory (Windows Auth)** → must migrate to OIDC/OAuth 2.0. Options: Amazon Cognito with AD federation, or AWS Managed Microsoft AD + LDAP. This is often the hardest part of the EBA. **OIDC/OAuth/SAML already** → minimal change, update redirect URIs. **ASP.NET Identity with SQL** → port Identity tables to PostgreSQL during DB migration. |
| 6.2 | Authorization model | Per app | **Role-based** → map to claims in the new IdP. **Custom** → may need code changes during EBA. Document the authorization rules before the 2-day. |
| 6.3 | Encryption requirements | Per app | **At-rest encryption** → Aurora PostgreSQL supports TDE equivalent via KMS. **Field-level** → must be handled in application code, verify libraries work on .NET LTS/Linux. |

---

## Section 7 — Hosting, Scaling & Resilience → Infrastructure Sizing & Architecture

| Q# | Question | Scope | How It Drives EBA |
|----|----------|-------|-------------------|
| 7.1 | Current hosting & container usage | Per app | Establishes the migration starting point. **On-prem VMs** → need VPN/Direct Connect for hybrid during transition. **Azure** → cross-cloud migration considerations. **Already containerized (Docker)** → containerization work is largely done, focus on ECS deployment config and orchestration. **Kubernetes/OpenShift** → target EKS instead of ECS if the team wants to keep Kubernetes; otherwise migrate to ECS Fargate for simplicity. **ECS already** → minimal container work, focus on app/DB modernization only. |
| 7.2 | Hardware specs | Per instance | Drives ECS task sizing (vCPU, memory). Right-size based on actual utilization, not allocated specs. |
| 7.3 | Traffic pattern | Per app | **Steady** → fixed ECS task count. **Peaks** → auto-scaling policies with scheduled scaling. **Spiky** → aggressive auto-scaling + Fargate Spot for cost optimization. |
| 7.4 | HA / DR mechanism | Per app | **Already HA** → must match or exceed in AWS (multi-AZ ECS, Aurora multi-AZ). **No HA** → opportunity to improve, but keep EBA scope simple — single AZ first, HA as stretch. |
| 7.5 | SLA / RPO / RTO | Per app | Drives Aurora backup configuration, ECS deployment strategy, and whether multi-region is needed. **RPO=0** → Aurora Global Database. **RTO < 1hr** → multi-AZ with automated failover. |
| 7.6 | Monitoring tools | Per app | Determines observability migration. **SCOM** → replace with CloudWatch + OpenTelemetry. **Datadog/AppDynamics** → can keep, add AWS integration. Stretch goal for EBA. |
| 7.7 | Sandbox environment | Per app | **Yes** → EBA can proceed. **No** → BLOCKER. Must be resolved in weeks 1-3 before EBA Party. The entire application stack must be runnable in isolation. If dependencies can't be sandboxed, mock interfaces must be built. |

---

## Section 8 — Testing & Code Quality → Validation Strategy

| Q# | Question | Scope | How It Drives EBA |
|----|----------|-------|-------------------|
| 8.1 | Unit / integration tests | Per project | **Tests exist** → run them after porting to validate correctness. This is the primary EBA validation mechanism. **No tests** → must define manual smoke tests before EBA. |
| 8.2 | Code coverage | Per project | **> 60%** → high confidence in automated validation. **< 30%** → allocate more time for manual testing during EBA. **0%** → critical risk, define acceptance criteria manually. |
| 8.3 | Load / performance / E2E tests | Per app | **Exist** → run post-migration to compare performance baselines. **Don't exist** → basic load test with k6 or Artillery during EBA as stretch goal. |
| 8.4 | Test frequency | Per app | Indicates CI maturity. **Every commit** → integrate into new CodePipeline. **Manual** → opportunity to automate during EBA (stretch goal). |

---

## Section 9 — DevOps & Environments → CI/CD Pipeline Design

| Q# | Question | Scope | How It Drives EBA |
|----|----------|-------|-------------------|
| 9.1 | Source control | Per app | **Git** → straightforward. **TFS/TFVC** → may need to migrate to Git first (pre-EBA task). Source must be in Git for CodePipeline integration. |
| 9.2 | CI/CD pipeline | Per app | **Exists** → can extend or replace with CodePipeline. **None** → building CI/CD is a stretch goal for EBA. |
| 9.3 | Deployment process | Per app | **Manual** → high value in automating during EBA. **Automated** → adapt existing pipeline to deploy to ECS. |
| 9.4 | Environments | Per app | Maps to AWS account/environment strategy. Identical environments → use CDK/CloudFormation for consistency. |
| 9.5 | Deployment frequency | Per app | **Daily/weekly** → team is agile, EBA will be smoother. **Quarterly** → team may need more guidance during the 2-day. Also serves as a baseline metric — post-modernization deployment frequency should improve. |
| 9.6 | Container / Linux experience | Per team | **Yes** → team can contribute to Dockerfile creation and ECS config during EBA. **No** → AWS team leads containerization, customer team observes and learns. Allocate training time in weeks 1-2. |

---

## Section 10 — Team & Readiness → EBA Staffing & Risk

| Q# | Question | Scope | How It Drives EBA |
|----|----------|-------|-------------------|
| 10.1 | In-house or vendor | Per app | **Vendor-maintained** → vendor must participate in EBA or provide full knowledge transfer before. **In-house** → direct access to developers. |
| 10.2 | Source code available | Per app | **No** → BLOCKER. Cannot proceed with EBA without source code access. Must be resolved in week 1. |
| 10.3 | Number of developers | Per app | Determines EBA team size. Ideally 2-4 developers from customer side for the 2-day. |
| 10.4 | .NET proficiency | Per team | **Advanced** → team can pair-program during EBA. **Beginner** → AWS team drives, customer observes. Factor into sprint planning. |
| 10.5 | PostgreSQL experience | Per team | **None** → allocate training time in weeks 1-2. Provide Aurora PostgreSQL cheat sheet for SQL Server developers. |
| 10.6 | SMEs for EBA | Per app | Must have: app developer, DBA, infra/DevOps. Missing any role → risk to EBA success. Business owner on standby for decisions. |
| 10.7 | Known blockers | Per app | Document and triage. Blockers must be resolved before EBA Party. Risks should have mitigation plans. |

---

## Section 11 — Modernization Expectations & Target State → Proof of Value & Success Criteria

| Q# | Question | Scope | How It Drives EBA |
|----|----------|-------|-------------------|
| 11.1 | Customer's own effort estimate | Per app | **Baseline for proof of value.** If customer estimates 12 months with 5 developers, and EBA achieves a working pilot in 6 weeks with 2-3 people, that's a compelling story. Document this before and after. |
| 11.2 | Preferred target architecture | Per app | Validates alignment with AWS recommendation. **ECS/Fargate** → default path. **EKS** → only if customer has Kubernetes experience (see 9.6). **Lambda** → only for event-driven components. **EC2 Linux** → fallback if containers are too complex for the pilot. |
| 11.3 | Success criteria | Per app | Defines the EBA demo acceptance criteria. Must be agreed before the 2-day. Examples: "App running on ECS with Aurora PostgreSQL, passing all unit tests, accessible via ALB." |
| 11.4 | Annual licensing costs | Per app | **Proof of value calculation.** Windows Server + SQL Server Enterprise licensing can be $50K–$500K+/year. Moving to Linux containers + Aurora PostgreSQL eliminates these costs. Include in the EBA debrief presentation. |

---

## Section 12 — Database → Data Migration Strategy

> **Non-SQL Server databases:** If the answer to Q12.1 is PostgreSQL, MySQL, file-based, or none — the database is either already on the target engine or doesn't require migration. Mark the remaining Section 12 questions as N/A and focus EBA scope on application modernization only. If the database is MySQL, consider Aurora MySQL as the target instead of Aurora PostgreSQL.

| Q# | Question | Scope | How It Drives EBA |
|----|----------|-------|-------------------|
| 12.1 | Database engine, version & edition | Per instance | **SQL Server Enterprise** → check feature usage (12.8) to determine if Standard suffices on Aurora. **SQL Server 2008/2012** → end of support, strong modernization driver. Version determines AWS Transform for SQL Server compatibility (2008 R2–2022 supported). **PostgreSQL/MySQL** → already on target or near-target engine, skip DB migration and focus on app modernization. **File-based/None** → no DB migration needed. |
| 12.2 | HA setup | Per instance | **Always On AG** → Aurora multi-AZ replaces this natively. **FCI** → Aurora handles failover. **None** → Aurora improves resilience for free. |
| 12.3 | Database size | Per database | **< 50 GB** → scripted migration (BCP → CSV → PostgreSQL COPY) is viable if app is a rewrite; see Database Migration Tooling Decision Framework. **50 GB–1 TB** → use DMS for data migration. **> 1 TB** → start DMS replication early (week 2). **> 10 TB** → may need Snowball or extended replication window, likely out of 2-day EBA scope. |
| 12.4 | Workload type | Per database | **OLTP** → Aurora PostgreSQL is ideal. **Data Warehouse** → consider Amazon Redshift instead. **Reporting** → Aurora read replicas or QuickSight. **Mixed** → may need to split workloads. |
| 12.5 | Data access layer | Per project | **Entity Framework 6** → AWS Transform for SQL Server handles EF migration (connection strings, DbContext, LINQ) when porting. **ADO.NET / raw SQL** → if porting, more manual T-SQL → PL/pgSQL conversion needed (Kiro or SCT). If rewriting, build new data access layer natively for PostgreSQL. **Dapper** → update SQL queries with Kiro. **Classic ADO (COM)** → full rewrite required, build new data access layer from scratch. |
| 12.6 | DB object counts & business logic in DB objects | Per database | Object counts size the migration effort: **Tables < 50, SPs < 10** → low complexity, Kiro + AI-DLC can handle conversion directly. **Tables 50–200, SPs 10–100** → moderate, use SCT or AWS Transform for SQL Server for bulk conversion. **Tables 200+, SPs 100+** → high complexity, expect significant manual review even with tooling. **Triggers > 20 or Functions > 50** → flag for careful testing, these often contain hidden business logic. If > 50% of business logic lives in DB objects, database migration dominates EBA scope. Use SCT or AWS Transform for SQL Server to automate T-SQL → PL/pgSQL when porting. For rewrites with simple SPs, Kiro + AI-DLC handles conversion directly. |
| 12.7 | SSIS / SSRS / SSAS | Per instance | **SSIS** → replace with AWS Glue or Step Functions. **SSRS** → replace with QuickSight or keep on separate SQL Server. **SSAS** → out of EBA scope, plan separately. Each component service is a separate migration workstream. |
| 12.8 | Feature checklist (Yes/No) | Per instance | Each "Yes" needs a mitigation plan. See feature migration matrix below. |
| 12.9 | Authentication | Per instance | **Windows Auth** → must switch to SQL Auth or IAM Auth for Aurora. Plan credential migration. |
| 12.10 | Backup & sync | Per instance | **CDC/Replication** → may need DMS CDC for cutover. **ETL** → redesign for Aurora. Backup strategy maps to Aurora automated backups + snapshots. |
| 12.11 | Concurrent connections | Per instance | Drives Aurora instance sizing and whether RDS Proxy is needed. **> 500 connections** → use RDS Proxy for connection pooling. **< 100** → direct connection is fine. |
| 12.12 | Cutover / maintenance window & data freshness | Per instance | Defines the migration cutover strategy. **Narrow window** → must use DMS CDC for continuous replication, cutover during the window with minimal downtime. **Flexible window (hours tolerable)** → scripted migration (BCP/COPY) is viable for databases < 50 GB; DMS full-load for larger databases. **Data freshness requirements** → drives whether DMS CDC is needed to keep source and target in sync during migration. |
| 12.13 | SCT assessment report | Per instance | **Available** → review conversion complexity score. Green items = automated. Yellow = manual review. Red = rewrite. This directly sizes the DB migration effort for EBA. **Not needed** if app is a full rewrite with a small, simple database (< 50 GB, few SPs) — Kiro + AI-DLC handles schema and SP conversion directly. See Database Migration Tooling Decision Framework. |

### SQL Server Feature Migration Matrix (Question 12.8)

| Feature | Aurora PostgreSQL Equivalent | EBA Action |
|---------|----------------------------|------------|
| CLR assemblies | PL/Python, PL/Perl, or Lambda UDF | Rewrite CLR code. High effort — may defer to post-EBA. |
| Linked Servers | `tds_fdw` (foreign data wrapper for SQL Server) | Install `tds_fdw` extension on Aurora PostgreSQL. Create foreign server, user mappings, and foreign tables for each referenced SQL Server table. Read-only (SELECT only). Requires network connectivity (VPN/Direct Connect) to SQL Server. |
| SQL Agent Jobs | Amazon EventBridge + Lambda or pg_cron | Rewrite job logic. Map schedules to EventBridge rules. |
| Replication (CDC) | DMS CDC or Aurora logical replication | Use DMS for migration, Aurora native replication post-migration. |
| TDE / SQL Audit | Aurora encryption at rest (KMS) + pgAudit | Enable KMS encryption, install pgAudit extension. Low effort. |
| Table Partitioning | PostgreSQL native partitioning | Recreate partition schemes. SCT handles most cases. |
| Memory-Optimized Tables | No direct equivalent | Redesign as regular tables with proper indexing. |
| Spatial data types | PostGIS extension | Install PostGIS, update spatial queries. Medium effort. |
| Full-Text Search | PostgreSQL tsvector/tsquery or OpenSearch | Built-in FTS for simple cases, OpenSearch for complex. |
| Database Mail | Amazon SES + Lambda | Replace with SES integration. |

### Database Migration Tooling Decision Framework

The choice of database migration tooling depends on the application modernization approach (port vs. rewrite), database size, schema complexity, and cutover requirements. Use this decision matrix to select the right tooling combination:

#### Tooling Selection by Scenario

| Scenario | Schema Conversion | Data Migration | Stored Procedure Conversion | When to Use |
|----------|------------------|----------------|---------------------------|-------------|
| **Kiro + AI-DLC only** | Kiro converts T-SQL DDL → PostgreSQL DDL | Scripted: BCP export → CSV → PostgreSQL `COPY` | Kiro converts T-SQL → PL/pgSQL | App is a full rewrite (no existing .NET data access to transform), DB < 50 GB, simple schema (few SPs/functions), flexible cutover window |
| **AWS SCT + DMS** | SCT automates schema conversion with assessment report | DMS handles data migration + optional CDC for continuous replication | SCT converts SPs (automated for simple cases, manual review for complex) | App is being ported (not rewritten), DB 50 GB–1 TB, moderate schema complexity, or need for continuous replication during transition |
| **AWS Transform for SQL Server + DMS** | AWS Transform handles full-stack conversion (schema + app data access layer) | DMS handles data migration + CDC | AWS Transform automates T-SQL → PL/pgSQL alongside EF/ADO.NET code changes | App is being ported with existing .NET data access layer (EF6, ADO.NET, SqlClient), DB any size, want automated end-to-end conversion of both DB and app code |
| **AWS SCT + DMS + Kiro** | SCT for schema, Kiro for complex SP rewrites SCT can't handle | DMS for data migration + CDC | Combined: SCT for bulk conversion, Kiro for complex/custom logic | App is being ported, DB > 50 GB, complex schema (hundreds of SPs, CLR, advanced features), tight cutover window |

#### Decision Criteria

| Factor | Kiro + AI-DLC Only | Bring in SCT/DMS | Bring in AWS Transform for SQL Server |
|--------|-------------------|-----------------|--------------------------------------|
| **App approach** | Full rewrite (no existing .NET data access code) | Port or rewrite | Port with existing .NET data access layer |
| **Database size** | < 50 GB (scripted BCP/COPY is fast enough) | > 50 GB (DMS handles large volumes efficiently) | Any size |
| **Stored procedures** | Few and simple | Many or complex (SCT assessment quantifies effort) | Alongside EF/ADO.NET code that also needs conversion |
| **Cutover window** | Flexible (hours tolerable) | Tight (need CDC for near-zero downtime) | Any |
| **Team overhead** | Minimal — no additional tools to learn | Moderate — SCT install, DMS instance provisioning | Moderate — AWS Console workflow |
| **Advanced SQL features** | None (no CLR, Linked Servers, SSIS) | Some (SCT handles common patterns) | EF/ADO.NET patterns in app code |

#### Scripted Data Migration Approach (for Kiro + AI-DLC only scenario)

When the database is small enough for scripted migration (< 50 GB):

1. **Export:** BCP (Bulk Copy Program) exports each table from SQL Server to CSV files
2. **Transform:** Kiro generates any needed data transformation scripts (data type adjustments, encoding)
3. **Import:** PostgreSQL `COPY` command bulk-loads CSV files into Aurora PostgreSQL
4. **Validate:** Row counts, checksums, and spot-check queries to verify data integrity

Performance reference: PostgreSQL `COPY` ingests CSV at hundreds of MB/s. A 3.5 GB database transfers in under 15 minutes. A 50 GB database would take approximately 1–2 hours including validation.


---

## EBA Pilot Selection Criteria

Based on questionnaire responses, score each application to select the best EBA pilot candidate:

| Criterion | Ideal for EBA Pilot | Score Weight |
|-----------|-------------------|-------------|
| Project type (2.1) | ASP.NET MVC or Web API (no Web Forms, WinForms, WPF, WF) | High |
| Third-party UI components (4.2) | None, or non-UI only | High |
| .NET version (2.4) | .NET Framework 3.5+ (AWS Transform sweet spot is 4.6.2+) | High |
| LoC (2.6) | < 50K total (up to 100K if well-structured) | High |
| Windows dependencies (2.9) | Mostly "No" answers | High |
| Isolation (4.5–4.8) | Minimal upstream/downstream dependencies — no more than 1–2 apps depend on it | High |
| ASMX/SOAP exposure (4.4) | No legacy SOAP endpoints consumed by other apps, or REST-only | High |
| Tech debt (2.8) | Low to Moderate | Medium |
| Test coverage (8.2) | > 30% | Medium |
| Team readiness (10.4) | Intermediate or Advanced | Medium |
| Business criticality (1.4) | Medium (not too risky, still meaningful) | Medium |
| DB complexity (12.6) | Limited stored procedure logic | Medium |
| Sandbox available (7.7) | Yes | Required |
| Source code available (10.2) | Yes | Required |

**Indicators that an app is better suited for a full modernization project (not EBA):**
- WinForms, WPF, XBAP, or Windows Workflow Foundation as primary project type — these require a build-new approach
- 100K LoC — the scope exceeds what a 6-week EBA can realistically deliver
- ASMX/SOAP endpoints consumed by 3+ downstream applications — backward compatibility adds significant scope and risk
- No source code access
- No sandbox environment
- Heavy COM/COM+ dependencies with no mitigation path
- Vendor-maintained components with no vendor participation or .NET on Linux support

---

## EBA Execution Timeline (6 Weeks)

### Weeks 1–2: Assessment & Planning
- Review questionnaire responses using this playbook
- Score applications and select EBA pilot (if not already selected)
- Run AWS Transform for .NET assessment (if app is being ported, not rewritten)
- Select database migration tooling using the Database Migration Tooling Decision Framework above
- If using SCT: run AWS SCT assessment on SQL Server
- If using Kiro only: extract SQL Server schema (DDL) and stored procedures, begin conversion with Kiro + AI-DLC
- Identify blockers from Sections 2.9, 5.8, 7.7 and begin mitigation
- Set up AWS account, VPC, ECR, and base infrastructure (CDK)
- Ensure sandbox environment is ready

### Weeks 3–4: Pre-EBA Preparation
- Run AWS Transform for .NET to generate transformation plan (if applicable)
- Begin automated code conversion (AWS Transform) or rewrite (Kiro + AI-DLC)
- Address critical blockers (Windows dependencies, auth migration design)
- Set up CI/CD pipeline skeleton (CodePipeline + CodeBuild)
- Database migration preparation:
  - If using DMS: start DMS replication for large databases (if > 50 GB)
  - If using Kiro only: prepare and test data migration scripts (BCP → CSV → PostgreSQL COPY)
  - If using AWS Transform for SQL Server: run transformation job
- Prepare Dockerfile and ECS task definitions
- Conduct dry-run of containerized app locally

### Week 5: Final Prep
- Validate all pre-requisites from [EBA Preparation Checklist](EBA-Preparation-Checklist.md)
- Confirm all SMEs are available for the 2-day
- Prepare EBA backlog (prioritized list of work items)
- Define demo script and success criteria (from 11.3)
- Ensure all tools are installed and configured on developer machines

### Week 6: EBA Party (2 Days)

**Day 1 — Build & Deploy**
| Time | Activity | Owner |
|------|----------|-------|
| 09:00 | Kickoff — review architecture, backlog, success criteria | AWS + Customer |
| 09:30 | .NET porting — apply AWS Transform output, fix remaining issues with Kiro | App Developers |
| 09:30 | DB migration — apply SCT/Transform output, validate schema | DBA |
| 12:00 | Lunch break | |
| 13:00 | Containerization — Dockerfile, ECS task definition, deploy to dev | DevOps + AWS |
| 14:00 | Integration — connect app to Aurora, update connection strings | App Dev + DBA |
| 15:00 | Auth migration — configure Cognito/OIDC or update auth middleware | AWS + App Dev |
| 16:00 | Smoke testing — run unit tests, manual validation | All |
| 17:00 | Day 1 retro — status check, plan Day 2 priorities | All |

**Day 2 — Validate & Demo**
| Time | Activity | Owner |
|------|----------|-------|
| 09:00 | Fix Day 1 issues, address remaining backlog items | All |
| 10:00 | CI/CD pipeline — CodePipeline + CodeBuild + ECR (stretch) | DevOps + AWS |
| 11:00 | Observability — CloudWatch logs, basic dashboards (stretch) | AWS |
| 12:00 | Lunch break | |
| 13:00 | End-to-end testing, performance baseline | All |
| 14:00 | Demo preparation | All |
| 15:00 | Demo to stakeholders | All |
| 16:00 | Retrospective, next steps, modernization roadmap discussion | All |
| 17:00 | Close | |

---

## Proof of Value Metrics

Capture these before and after the EBA to demonstrate value:

| Metric | Before (from Questionnaire) | After (EBA Result) |
|--------|---------------------------|-------------------|
| Estimated modernization effort (11.1) | Customer's estimate (e.g., 12 months, 5 devs) | Actual: 6 weeks, 2-3 devs |
| Deployment frequency (9.5) | e.g., Monthly | e.g., On-demand (CI/CD) |
| Deployment time | e.g., 4 hours manual | e.g., 15 minutes automated |
| Platform | Windows Server / IIS / SQL Server | Linux / ECS Fargate / Aurora PostgreSQL |
| Scalability | Manual, vertical | Auto-scaling, horizontal |
| HA/DR | Manual failover or none | Multi-AZ automated failover |

