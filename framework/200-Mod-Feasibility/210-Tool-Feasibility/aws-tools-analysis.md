# AWS Tool Eligibility & Modernization Pathways

This file defines the tool eligibility checks and modernization pathway recommendations for Step 2 of the feasibility analysis. It is maintained separately because AWS tools and services evolve rapidly.

> **Important — Verify with latest documentation.**
> AWS modernization services evolve frequently. The eligibility criteria below reflect the state of AWS Transform documentation as of early 2026, but may lag behind the latest service updates. Before finalizing tool eligibility for a customer engagement, verify the current capabilities using one of these approaches:
>
> **Option A — AWS Knowledge MCP Server (recommended for agentic IDEs)**
> If you are running this analysis in an agentic coding IDE such as Kiro, check if the [AWS Documentation MCP Server](https://awslabs.github.io/mcp/) is configured and enabled to get real-time access to the latest AWS documentation. If not configured, prompt the user to do so. 
> Then, when running the feasibility analysis, search for:
> - `AWS Transform .NET supported project types and versions`
> - `AWS Transform SQL Server requirements and limitations`
> - `AWS Transform Custom capabilities`
> - `AWS DMS Schema Conversion generative AI SQL Server PostgreSQL`
> - `AWS Database Migration Service SQL Server migration`
>
> Use the search results to validate and override the eligibility criteria below where they differ.
>
> **Option B — Manual check**
> Review the [AWS Transform User Guide](https://docs.aws.amazon.com/transform/latest/userguide/) directly.

## Shorthands

| Shorthand | Full Name |
|-----------|-----------|
| ATX | AWS Transform |
| ATX .NET | AWS Transform for .NET |
| ATX SQL | AWS Transform for SQL Server |
| ATX Custom | AWS Transform Custom |
| DMS | AWS Database Migration Service |
| DMS SC / SCT | DMS Schema Conversion (also known as Schema Conversion Tool) |

These shorthands are used throughout this document and in customer deliverables.

---

## CHECK 1 — AWS Transform for .NET

Ports .NET Framework applications to cross-platform .NET (targets: .NET 8 or .NET 10).

### GA Supported

Available in US East (N. Virginia) and Europe (Frankfurt).

Source versions:
- .NET Framework 3.5+
- .NET Core 3.1
- .NET 5.x+
- .NET 8

Languages: C# only.

Supported project types:
- Class Libraries
- Console Applications
- ASP.NET MVC (including front-end Razor Views)
- ASP.NET SPA back-ends (business logic layers)
- ASP.NET Web API
- ASP.NET Web Forms (→ Blazor Server on ASP.NET Core)
- Unit Test projects (NUnit, xUnit, MSTest)
- WCF Services
- Projects with cross-platform NuGet package equivalents (best-effort if missing)

UI porting:
- MVC Razor Views → ASP.NET Core Razor Views (automated)
- Web Forms → Blazor Server (automated, up to 1000 pages)

**3rd party UI component blockers** — applies to both WebForms and MVC:

Vendors like Telerik (Kendo UI), DevExpress (DevExtreme), Infragistics, ComponentOne, Syncfusion, and similar provide .NET Framework-specific server controls (WebForms), MVC HTML helpers / tag helpers, and JavaScript UI component wrappers that are tightly coupled to their framework-specific versions. AWS Transform cannot automatically convert these because:
- WebForms server controls (e.g. `<telerik:RadGrid>`) have no Blazor equivalent that Transform can generate
- MVC helpers (e.g. `@Html.Kendo().Grid()`) and tag helpers reference .NET Framework-specific assemblies and APIs
- JavaScript UI wrappers (e.g. Kendo UI for jQuery, DevExtreme) may have framework-specific server-side integration code

If the application uses any of these, the UI layer requires manual remediation: either upgrade to the vendor's ASP.NET Core / Blazor-compatible version (if available), or replace with an alternative UI library. AWS Transform will still port the non-UI business logic and back-end code.

Additional Web Forms blockers: .asmx files, .svc files, .ashx files, service/web references, mixed .aspx + .cshtml files

Source repos:
- Web experience (large-scale, portfolio-level porting): Requires CodeConnections — GitHub, GitHub Enterprise, GitLab.com, Bitbucket Cloud, Azure Repos.
- IDE experience (Visual Studio 2022/2026): Works with any local codebase regardless of source control. Use this for repos in TFS, SVN, file shares, or any unsupported SCM — simply open the solution locally and run ATX .NET from the AWS Toolkit.

TFS/SVN is therefore not a blocker for ATX .NET eligibility — it only limits you to the IDE experience (one repo at a time) rather than the web experience (batch/portfolio).

IDE support: AWS Toolkit for Visual Studio 2022/2026 provides an interactive, editable transformation plan with real-time progress tracking.

### Preview

Available in US East (N. Virginia) only:
- WinForms desktop projects
- WPF desktop projects
- Xamarin mobile projects (→ MAUI)
- VB.NET projects

### Not Supported
- Blazor UI components (already cross-platform)
- Win32 DLLs without core-compatible libraries
- Repositories without .sln files
- C++, F#

### Classification

| Classification | Criteria |
|---------------|----------|
| GA ELIGIBLE | C#, supported project type, .NET Framework 3.5+ or .NET Core 3.1 or .NET 5+/8, source in supported repo (web experience) |
| GA ELIGIBLE (IDE ONLY) | Same as above but source in TFS/SVN/file share — use Visual Studio IDE experience instead of web experience |
| PREVIEW ELIGIBLE | WinForms, WPF, Xamarin, or VB.NET project |
| ELIGIBLE WITH PREREQUISITES | Missing NuGet packages need resolution, or other pre-work required |
| NOT ELIGIBLE | C++, F#, Blazor, Win32 DLL, no .sln, no source code |

---

## CHECK 2 — AWS Transform for SQL Server

AI-powered full-stack modernization of SQL Server databases and associated .NET applications to Amazon Aurora PostgreSQL. Handles schema conversion, stored procedure translation, data migration (via DMS), and application code updates (connection strings, EF providers).

### Requirements

Available in US East (N. Virginia) only.

- SQL Server 2008 R2 through 2022 (all editions: Express, Standard, Enterprise)
- Application must be on .NET 6+ (port with ATX .NET first if on .NET Framework)
- Entity Framework 6.3–6.5, EF Core 1.0–8.0, or ADO.NET
- Source in supported repo via CodeConnections (GitHub, GitHub Enterprise, GitLab.com, Bitbucket Cloud, Azure Repos)
- DB user needs VIEW DEFINITION and VIEW DATABASE STATE permissions
- DB passwords must use printable ASCII characters only (excluding `/`, `@`, `"`, and spaces)
- VPC containing the source SQL Server must have subnets in at least 2 different Availability Zones (required for DMS replication subnet groups)

Database hosting requirements:
- **AWS-hosted (RDS SQL Server or SQL Server on EC2):** The database and ATX SQL must be in the same AWS Region (currently us-east-1). If the database is in a different region, clone it to us-east-1 for transformation, then deploy results back to the target region.
- **Externally hosted (on-premises, Azure, other cloud):** Supported, but requires network connectivity between the database and the ATX SQL service in us-east-1 (e.g., via VPN, Direct Connect, or public endpoint).

Target: Amazon Aurora PostgreSQL (PostgreSQL 15+ compatible).

### Not Handled

- SSIS (use AWS Glue or Step Functions)
- SSRS (use Amazon QuickSight)
- SSAS (use Amazon Redshift or Athena)
- CLR Assemblies (rewrite as .NET logic or Lambda)
- Linked Servers (reconfigure post-migration or refactor to API calls / RDS Proxy)
- Cross-database dependencies (requires manual decoupling)
- .NET Framework 4.x and earlier (port to .NET 6+ first)
- VB.NET applications

### Classification

| Classification | Criteria |
|---------------|----------|
| ELIGIBLE | App on .NET 6+, SQL Server 2008 R2–2022, supported ORM, supported repo |
| ELIGIBLE AFTER .NET PORTING | App on .NET Framework (port with .NET agent first), DB otherwise qualifies |
| PARTIALLY ELIGIBLE | DB qualifies but has SSIS/SSRS/SSAS/CLR/Linked Servers that need manual remediation, or DB is not yet on AWS (migrate to RDS/EC2 first using DMS or native backup/restore) |
| NOT APPLICABLE | No SQL Server database, or database is already PostgreSQL/MySQL/other |
| NOT ELIGIBLE | VB.NET app, no source code, unsupported repo with no migration path |

### Two-Step Dependency

Only applies when the source application is on a .NET version older than the minimum supported by ATX SQL (currently .NET 6). In that case, ATX .NET must run first to port the application to .NET 6+, then ATX SQL can process the database modernization. If the application is already on .NET 6+, ATX SQL can be used directly without ATX .NET.

---

## CHECK 3 — ATX Custom & Kiro (Agentic Code Modernization)

Both ATX Custom and Kiro are agentic AI tools that can perform code modernization work. They serve different sweet spots depending on scale and repeatability.

Available in US East (N. Virginia).

### ATX Custom — Repeatable Transformations at Scale

ATX Custom is designed for scenarios where the same transformation pattern needs to be applied consistently across many codebases, components, or projects. It shines when you can define a transformation once and execute it repeatedly.

Key concepts:
- **Transformation definitions**: A package of instructions (natural language + reference docs + code samples) that describes a specific transformation. Stored in a per-account registry, versioned, and shareable across teams.
- **Before-and-after code samples**: ATX Custom works best when you can provide example input/output code. A practical approach is to modernize 1–2 applications first (e.g., using Kiro), then use the before vs. after code to define and teach the ATX Custom transformation definition.
- **Continual learning**: The agent automatically extracts knowledge items from every execution and developer feedback. These accumulate over time, improving transformation quality as more repos are processed. Knowledge items are transformation-specific and account-specific.
- **Bulk execution**: Supports non-interactive/headless mode for CI/CD pipeline integration, enabling batch processing across many repos without human intervention.
- **Build/validation commands**: Optional but recommended — lets the agent validate transformed code during execution, feeding results back into the learning loop.

Best for:
- Repeatable patterns across 10+ codebases (e.g., replacing a deprecated internal library, upgrading a shared framework version, standardizing coding patterns)
- Organization-specific transformations (proprietary framework migrations, internal API upgrades)
- Supplementary transformations after ATX .NET / ATX SQL completes (e.g., upgrading remaining dependencies, refactoring patterns the main agents don't cover)
- Components the .NET/SQL agents cannot handle that follow a consistent pattern (CLR Assembly rewrites, complex T-SQL patterns)

Workflow:
1. **Pilot with Kiro** — Modernize 1–2 representative applications manually using Kiro to establish the before/after pattern
2. **Define transformation** — Use the pilot output as reference material to create an ATX Custom transformation definition (natural language instructions + before/after code samples + reference docs)
3. **Test and refine** — Run the transformation on a few more repos, review results, provide feedback. The continual learning system improves quality with each iteration.
4. **Scale execution** — Publish the transformation and execute across the remaining portfolio via CLI (interactive or headless)

### Kiro — Per-Application Modernization

Kiro is an agentic coding IDE that can perform the same types of code transformations as ATX Custom, but is oriented toward interactive, per-application work. It excels at understanding application context, making nuanced decisions, and handling one-off or complex modernization tasks that don't follow a repeatable pattern.

Best for:
- First 1–2 pilot applications (establishing the modernization pattern before scaling with ATX Custom)
- Complex, one-off modernization work where each application has unique challenges
- Architectural redesign (e.g., decomposing a monolith, replacing BizTalk with Step Functions)
- Post-transformation cleanup and refinement (addressing NextSteps.md items from ATX .NET)
- Applications where the developer needs to make judgment calls throughout the process

### When to Use Which

| Scenario | Recommended Tool |
|----------|-----------------|
| Same transformation pattern across many repos/projects | ATX Custom (define once, execute at scale) |
| First 1–2 pilot modernizations to establish the pattern | Kiro (then use output to define ATX Custom transformation) |
| One-off complex modernization with unique challenges | Kiro |
| Architectural redesign or replacement (no repeatable pattern) | Kiro |
| Post-ATX .NET / ATX SQL cleanup and refinement | Kiro |
| Organization-wide coding standard enforcement | ATX Custom |
| Batch library/dependency upgrades across portfolio | ATX Custom |

### Supported Transformation Patterns (ATX Custom)

| Pattern | Examples |
|---------|----------|
| API and Service Migrations | AWS SDK v1→v2, javax→jakarta |
| Language Version Upgrades | Java 8→17, Python 3.9→3.13, Node.js 12→22 |
| Framework Upgrades | Spring Boot 2.x→3.x, React 17→18 |
| Framework Migrations | Angular→React, Redux→Zustand |
| Library and Dependency Upgrades | Pandas 1.x→2.x, Lodash upgrades |
| Code Refactoring | Pattern modernization, observability instrumentation |
| Script Translations | CDK→Terraform, Bash→PowerShell |
| Architecture Migrations | x86→Graviton, on-prem→Lambda |
| Custom Transformations | Internal library migrations, proprietary framework upgrades |

---

## CHECK 4 — AWS DMS & DMS Schema Conversion

AWS Database Migration Service (DMS) and its built-in Schema Conversion feature are the foundational database migration tools. They work independently of AWS Transform and cover scenarios where Transform for SQL Server is not applicable or where broader database migration flexibility is needed.

### DMS Schema Conversion (with Generative AI)

Converts database schemas and code objects (stored procedures, functions, triggers, views, synonyms, data types) from source to target format. The GenAI feature uses Amazon Bedrock foundation models to automate conversion of complex code objects, claiming up to 90% automation of the schema conversion process.

Supported source → target for this framework:
- SQL Server → Amazon Aurora PostgreSQL-Compatible
- SQL Server → Amazon RDS for PostgreSQL

GenAI-assisted conversion available in 13+ regions (as of March 2026): US East (N. Virginia, Ohio), US West (Oregon), Europe (Frankfurt, Ireland, London, Stockholm, Paris), Asia Pacific (Tokyo, Osaka, Sydney), Canada (Central). Expanding — verify latest region availability.

Key capabilities:
- Automated schema and code object conversion with GenAI recommendations
- Migration assessment reports estimating complexity and identifying objects needing manual intervention
- Transformation rules for renaming schemas, tables, columns during conversion
- Extension packs for emulating source-specific features not natively available in PostgreSQL
- Stub objects for unsupported built-in source objects

Limitations:
- No offline mode
- No SQL code editing for source within the tool
- No filtering of database objects (converts all or nothing per schema)
- Does not handle application code changes (connection strings, ORM config) — that's the app team's responsibility or use AWS Transform for SQL Server

### DMS Data Migration

Handles the actual data movement from source to target database.

Capabilities:
- Full load migration (one-time bulk copy)
- Change Data Capture (CDC) for ongoing replication (near-zero downtime migration)
- Full load + CDC combined for minimal cutover window
- Homogeneous migrations (SQL Server → RDS SQL Server) using native database tools — serverless, auto-scaling
- Heterogeneous migrations (SQL Server → Aurora PostgreSQL) using replication instances
- Data validation to verify source and target data consistency

Supports SQL Server as source (on-premises, EC2, RDS, Azure SQL) and targets including Aurora PostgreSQL, RDS PostgreSQL, RDS SQL Server.

### When to Use DMS vs AWS Transform for SQL Server

| Scenario | Recommended Tool |
|----------|-----------------|
| Full-stack modernization (.NET app + SQL Server → Aurora PostgreSQL) with app code changes | AWS Transform for SQL Server (uses DMS under the hood) |
| Database-only migration, no .NET app code changes needed (e.g., app already on .NET 6+ with abstracted data access) | DMS Schema Conversion + DMS Data Migration |
| SQL Server → RDS SQL Server replatform (keep SQL Server engine) | DMS homogeneous migration |
| App is NOT .NET (Java, Python, etc.) with SQL Server backend | DMS Schema Conversion + DMS Data Migration |
| AWS Transform for SQL Server not eligible (VB.NET, no source code, unsupported repo) but DB still needs migrating | DMS Schema Conversion + DMS Data Migration |
| SSIS/SSRS/SSAS components alongside the database | DMS for the database + manual remediation for components (see Manual Remediation Mapping) |
| Need CDC / ongoing replication during migration cutover | DMS Data Migration (CDC mode) |

### Classification

| Classification | Criteria |
|---------------|----------|
| DMS SCHEMA CONVERSION + DATA MIGRATION | SQL Server → Aurora PostgreSQL or RDS PostgreSQL, app code changes handled separately |
| DMS HOMOGENEOUS MIGRATION | SQL Server → RDS SQL Server (replatform, keep engine) |
| DMS DATA MIGRATION ONLY | Schema already converted or compatible, just need data movement and/or CDC |
| NOT APPLICABLE | No database, or database is not SQL Server |

---

## Modernization Pathways

Recommend one pathway per application based on the eligibility checks above. Each pathway describes the tools involved, the typical sequence of steps, and what manual/Kiro work to expect.

> **Note on manual remediation:** ATX agents may not achieve 100% automated conversion. Expect to use Kiro or manual developer effort for post-transformation cleanup in virtually every pathway that involves ATX. The ATX .NET agent generates a `NextSteps.md` file identifying remaining porting work; the ATX SQL agent flags schema objects needing human review. Factor this into every plan.

---

### Pathway 1 — ATX-Led Modernization

**When:** Application is eligible for ATX .NET and/or ATX SQL. This is the primary pathway for custom .NET applications with SQL Server databases. Covers three scenarios depending on what needs modernizing:
- App only (no SQL Server, or DB already on PostgreSQL/Aurora) → ATX .NET
- DB only (app already on .NET 6+) → ATX SQL
- Full-stack (app on .NET Framework + SQL Server) → ATX .NET first, then ATX SQL (two-step, only needed when app is below ATX SQL's minimum .NET version)

If the application has components ATX cannot handle (SSIS, SSRS, SSAS, CLR Assemblies, Linked Servers, etc.), use ATX for what it can handle and remediate the rest manually or with Kiro — see Manual Remediation Mapping below.

**Tools:** ATX .NET + ATX SQL + DMS (under the hood for data migration) + Kiro + ISV tools if needed

**Steps:**
1. ATX .NET agent: port .NET Framework → .NET 8/10 (web experience for supported repos, IDE experience for TFS/SVN/local)
2. Review transformation summary and `NextSteps.md` — address remaining items using Kiro (Linux readiness, NuGet resolution, 3rd party UI component upgrades)
3. ATX SQL agent (if SQL Server → Aurora PostgreSQL): connect database, run assessment, review wave plan, execute schema conversion + data migration + app code updates
4. Review SQL agent flagged items — address using Kiro (complex T-SQL, stored procedure validation)
5. Remediate unsupported components manually or with Kiro (SSRS → QuickSight, SSIS → Glue, CLR → rewrite, etc.)
6. Validate end-to-end, deploy through CI/CD pipeline

**Manual/Kiro work:** Post-transformation cleanup after each ATX step. For WebForms → Blazor, expect UI polish and 3rd party control replacement. For MVC with vendor UI helpers (Kendo UI, DevExtreme), expect manual upgrade to ASP.NET Core versions. For SQL, expect complex T-SQL review and Enterprise feature equivalents (columnstore, in-memory OLTP). For unsupported components (SSRS, SSIS, SSAS, CLR, Linked Servers), see Manual Remediation Mapping.

**ISV tools (optional):** See `210-Tool-Feasibility/isv-tools-analysis.md` for detailed ISV tool guidance when the portfolio is large (20+ apps), contains VB.NET/VB6 code, or has complex monolithic applications needing decomposition.

---

### Pathway 2 — ATX Custom Only

**When:** Application is NOT eligible for ATX .NET or ATX SQL, but the transformation needed is straightforward and follows a well-defined pattern that ATX Custom can handle without the specialized .NET/SQL agents.

**"Straightforward" criteria** — ATX Custom works well standalone when:
- The transformation is a well-scoped pattern change (not a full framework port): language version upgrade, library/dependency upgrade, API migration, code refactoring, or script translation
- The codebase is relatively contained: < 100K LOC, or the changes are localized to specific files/patterns rather than cross-cutting
- Before-and-after examples exist or can be easily produced (e.g., from a manual pilot of 1–2 files)
- A build/validation command exists to verify the transformation automatically

**Common scenarios:**
- F# or C++ projects that ATX .NET doesn't support, but need targeted refactoring or dependency upgrades
- Batch upgrades of internal libraries, NuGet packages, or coding patterns across many small projects
- Script translations (PowerShell → Bash, CDK → Terraform)
- Post-ATX cleanup patterns that repeat across many repos (e.g., standardizing logging, adding observability instrumentation)

**Tools:** ATX Custom + Kiro (for pilot and edge cases)

**Steps:**
1. Pilot: manually modernize 1–2 representative files or projects using Kiro to establish the before/after pattern
2. Define ATX Custom transformation using the pilot output (natural language instructions + before/after code samples + reference docs)
3. Test on a few more repos, refine based on results (continual learning improves quality)
4. Scale execution across remaining targets via CLI (interactive or headless)
5. Kiro: address edge cases that the custom transformation doesn't handle

**Manual/Kiro work:** Kiro is essential for the pilot phase and for edge cases. After the transformation definition matures through a few iterations, manual effort decreases as the continual learning system improves quality.

---

### Pathway 3 — ATX .NET + DMS

**When:** .NET Framework application with SQL Server, but ATX SQL is not eligible (VB.NET app, unsupported repo for SQL agent, DB not accessible from us-east-1, etc.) — or the decision is to keep SQL Server as the engine (migrate to RDS SQL Server instead of Aurora PostgreSQL).

**Tools:** ATX .NET agent + DMS Schema Conversion (GenAI) + DMS Data Migration + Kiro

**Steps:**
1. ATX .NET agent: port .NET Framework → .NET 8/10
2. Kiro: address post-transformation items
3. DMS Schema Conversion (with GenAI): convert SQL Server schema → Aurora PostgreSQL / RDS PostgreSQL. Or use DMS homogeneous migration for SQL Server → RDS SQL Server.
4. DMS Data Migration: full load + CDC for near-zero downtime cutover
5. Kiro: update application connection strings and data access code (ATX SQL would have automated this, but it's not available in this pathway)
6. Validate end-to-end, deploy through CI/CD pipeline

**Manual/Kiro work:** Application data access code changes are manual since ATX SQL isn't handling them. DMS Schema Conversion GenAI handles up to 90% of schema conversion but expect manual review of complex stored procedures.

---

### Pathway 4 — DMS Only (Database Migration)

**When:** Database migration without application code modernization. Covers two scenarios:
- **Heterogeneous** (SQL Server → Aurora PostgreSQL / RDS PostgreSQL): Use DMS Schema Conversion (GenAI) + DMS Data Migration. For non-.NET apps, or when app code changes are handled separately.
- **Homogeneous** (SQL Server → RDS SQL Server): Use DMS homogeneous migration or native backup/restore. For replatforming to managed service while keeping the engine.

**Tools:** DMS Schema Conversion (GenAI) + DMS Data Migration, or native backup/restore

**Steps (heterogeneous):**
1. Create DMS migration project, run assessment report
2. Enable GenAI-assisted schema conversion, review and address unconverted objects
3. Apply converted schema, run data migration (full load + CDC if needed)
4. Validate data integrity, update application connection strings, cut over

**Steps (homogeneous):**
1. Provision RDS SQL Server (Standard edition preferred for cost)
2. DMS homogeneous migration (serverless, native tools) or native backup/restore via S3
3. Configure Multi-AZ for HA (replaces Always On AG / FCI)
4. Update connection strings, validate, cut over

**Manual/Kiro work:** Heterogeneous — manual review of unconverted schema objects, app connection string updates. Homogeneous — minimal, mainly connection strings and HA config.

---

### Pathway 5 — Kiro-Led Modernization

**When:** Application requires significant architectural judgment, one-off complex work, or doesn't fit any ATX agent's scope. Also the right choice for the first 1–2 pilot modernizations before defining ATX Custom transformations.

**Common scenarios:**
- Complex one-off modernization with unique challenges per application
- Post-ATX cleanup and refinement (addressing `NextSteps.md` items)
- Applications where the developer needs to make judgment calls throughout
- Pilot modernizations to establish before/after patterns for ATX Custom

**Tools:** Kiro

**Steps:**
1. Open the application in Kiro
2. Use Kiro's agentic capabilities to analyze, plan, and execute the modernization
3. Validate build, run tests, deploy through CI/CD pipeline

**Manual/Kiro work:** Kiro does the heavy lifting but the developer guides the process. Best for applications where context and judgment matter more than repeatability.

---

### Pathway 6 — Architectural Replacement

**When:** BizTalk, WCF Workflow Services, Windows Workflow Foundation (WF), legacy middleware, or other platforms with no direct port path. These require a ground-up redesign using AWS-native services.

**Tools:** Kiro + AWS services (Step Functions, EventBridge, Lambda, API Gateway, SQS, Amazon MQ)

**Steps:**
1. Inventory all existing orchestrations, pipelines, workflows, and integration points
2. Design replacement architecture using AWS-native services (strangler fig pattern recommended)
3. Build replacement services using Kiro — use existing orchestration definitions as specs
4. Migrate integrations one at a time, validate each before proceeding
5. Decommission legacy platform once all integrations are migrated

**Manual/Kiro work:** Most labor-intensive pathway. Kiro accelerates the build but architectural design and integration testing require significant developer effort. For BizTalk, evaluate AWS B2B Data Interchange for EDI scenarios.

---

### Pathway 7 — Replatform (Lift-and-Shift)

**When:** COTS products (SAP, Tableau, etc.), no source code available, application retiring soon, or modernization not justified.

**Tools:** AWS Application Migration Service (MGN) + DMS + RDS

**Steps:**
1. AWS MGN: lift-and-shift application servers to EC2 Windows
2. DMS or native backup/restore: migrate SQL Server to RDS SQL Server
3. Configure Multi-AZ for HA, AWS Systems Manager for patching
4. Update DNS, validate, cut over

**Manual/Kiro work:** Minimal code changes — infrastructure migration only.

---

### Pathway 8 — Retain / Defer

**When:** SaaS replacement planned, low business priority, end-of-life, or modernization not justified at this time.

**Action:** Keep as-is, revisit in future assessment cycle. Document the rationale for deferral.

---

## Manual Remediation Mapping

For components not handled by AWS Transform agents, use these AWS service equivalents:

| Legacy Component | AWS Replacement |
|-----------------|----------------|
| SSIS | AWS Glue, Step Functions |
| SSRS | Amazon QuickSight |
| SSAS | Amazon Redshift, Athena |
| CLR Assemblies | Rewrite as .NET logic, Lambda, or use AWS Transform Custom |
| Linked Servers | Refactor to API calls, RDS Proxy, or reconfigure post-migration |
| SQL Agent Jobs | Step Functions, EventBridge Scheduler, Lambda |
| MSMQ | Amazon SQS, SNS |
| COM/DCOM | Rewrite as .NET services or microservices |
| BizTalk | Step Functions + EventBridge + API Gateway |
| Windows Workflow (WF) | Step Functions |
| WCF (SOAP/TCP) | gRPC, REST API (ASP.NET Core Web API) |
| Office VSTO Add-ins | Office Add-ins (web-based), or retain on Windows |
