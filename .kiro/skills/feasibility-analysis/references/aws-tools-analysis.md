# AWS Tool Eligibility & Modernization Pathways

This file defines the tool eligibility checks and modernization pathway recommendations for Step 2 of the feasibility analysis. It is maintained separately because AWS tools and services evolve rapidly.

> **Important — Verify with latest documentation.**
> AWS modernization services evolve frequently. The eligibility criteria below reflect the state of AWS Transform documentation as of August 2026, but may lag behind the latest service updates. Before finalizing tool eligibility for a customer engagement, verify the current capabilities using one of these approaches:
>
> **Option A — AWS Knowledge MCP Server (recommended for agentic IDEs)**
> If you are running this analysis in an agentic coding IDE such as Kiro, check if the [AWS Documentation MCP Server](https://awslabs.github.io/mcp/) is configured and enabled to get real-time access to the latest AWS documentation. If not configured, prompt the user to do so.
> Then, when running the feasibility analysis, search for:
> - `AWS Transform .NET supported project types and versions`
> - `AWS Transform SQL Server requirements and limitations`
> - `AWS Transform Custom capabilities`
> - `AWS Transform continuous modernization`
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
| ATX CM | AWS Transform Continuous Modernization |
| DMS | AWS Database Migration Service |
| DMS SC / SCT | DMS Schema Conversion (also known as Schema Conversion Tool) |

These shorthands are used throughout this document and in customer deliverables.

---

## CHECK 1 — AWS Transform for .NET

Ports .NET Framework applications to cross-platform .NET (targets: .NET 8 or .NET 10).

### GA Supported

Available in 8 regions: US East (N. Virginia), Europe (Frankfurt), Europe (London), Asia Pacific (Mumbai), Asia Pacific (Sydney), Asia Pacific (Tokyo), Asia Pacific (Seoul), Canada (Central).

Source versions:
- .NET Framework 3.5+
- .NET Core 3.1
- .NET 5.x+
- .NET 8

Target versions:
- .NET 8 (LTS)
- .NET 10

Languages: C# only (GA). VB.NET in Preview.

Supported project types:
- Class Libraries (including .NET Standard targets)
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
- Entity Framework ORM code porting (automated)

**3rd party UI component blockers** — applies to both WebForms and MVC:

Vendors like Telerik (Kendo UI), DevExpress (DevExtreme), Infragistics, ComponentOne, Syncfusion, and similar provide .NET Framework-specific server controls (WebForms), MVC HTML helpers / tag helpers, and JavaScript UI component wrappers that are tightly coupled to their framework-specific versions. AWS Transform cannot automatically convert these because:
- WebForms server controls (e.g. `<telerik:RadGrid>`) have no Blazor equivalent that Transform can generate
- MVC helpers (e.g. `@Html.Kendo().Grid()`) and tag helpers reference .NET Framework-specific assemblies and APIs
- JavaScript UI wrappers (e.g. Kendo UI for jQuery, DevExtreme) may have framework-specific server-side integration code

If the application uses any of these, the UI layer requires manual remediation: either upgrade to the vendor's ASP.NET Core / Blazor-compatible version (if available), or replace with an alternative UI library. AWS Transform will still port the non-UI business logic and back-end code.

Additional Web Forms blockers: .asmx files, .svc files, .ashx files, service/web references, mixed .aspx + .cshtml files

### Experiences (Web vs IDE)

**Web console experience** — for large-scale portfolio-level transformation of up to 1,000 repositories per job. Supports connecting to source code via:
- CodeConnections: GitHub, GitLab, Bitbucket
- Amazon S3: upload source code as zip (alternative for repos not accessible via CodeConnections)

**IDE experience (Visual Studio 2022/2026)** — developer-led interactive transformation via AWS Toolkit for Visual Studio. Works with any local codebase regardless of source control. Offers:
- Autonomous mode: unattended transformation suitable for overnight/long-running jobs
- Interactive mode: conversational AI assistant, customizable transformation plan, step-by-step review, iterative refinement
- Editable transformation plan with estimated transformation time
- Real-time progress tracking via Worklog
- NextSteps.md output for handoff to AI code companions (Kiro)

Requires IAM Identity Center authentication.

TFS/SVN/file share is not a blocker — use the IDE experience (one solution at a time) or upload via S3 to the web experience. Both approaches are equally capable for transformation quality.

**Kiro / other AI code companions** — transform from Kiro using the AWS Transform Power, or from other agents using the AWS Transform MCP agent plugin.

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

### Quotas

| Quota | Default | Adjustable |
|-------|---------|------------|
| Monthly lines of code (web & IDE) | 2,000,000 per region | Yes |
| Concurrent IDE jobs per IAM Identity Center account | 10 per region | Yes |
| Concurrent web jobs per IAM Identity Center account | 5 per region | Yes |
| Max repositories per web job | 1,000 | No |

### Classification

| Classification | Criteria |
|---------------|----------|
| GA ELIGIBLE | C#, supported project type, .NET Framework 3.5+ or .NET Core 3.1 or .NET 5+/8, source in supported repo (web experience) or S3 |
| GA ELIGIBLE (IDE ONLY) | Same as above but source in TFS/SVN/file share — use Visual Studio IDE experience |
| PREVIEW ELIGIBLE | WinForms, WPF, Xamarin, or VB.NET project |
| ELIGIBLE WITH PREREQUISITES | Missing NuGet packages need resolution, or other pre-work required |
| NOT ELIGIBLE | C++, F#, Blazor, Win32 DLL, no .sln, no source code |

---

## CHECK 2 — AWS Transform for SQL Server

AI-powered full-stack modernization of SQL Server databases and associated .NET applications to Amazon Aurora PostgreSQL. Handles schema conversion, stored procedure translation (T-SQL → PL/pgSQL), data migration (via DMS), application code updates (connection strings, EF providers, ADO.NET), and automated deployment to ECS/EC2.

### Capabilities

**Database transformation:**
- Schema conversion: tables, views, indexes, constraints, relationships, triggers, functions, computed columns, identity columns
- Stored procedure transformation: T-SQL → PL/pgSQL with AI-enhanced accuracy
- Data migration with integrity validation using DMS
- Automated data integrity verification and referential integrity checks

**Application transformation:**
- Entity Framework 6.3–6.5 and EF Core 1.0–10.0 configuration updates for PostgreSQL
- ADO.NET data access code conversion (SQL Server → PostgreSQL providers)
- Connection string updates to target PostgreSQL database
- Database provider replacement (SQL Server → Npgsql)
- ORM configuration updates (data type mappings, identity columns, DB-specific configs)

**Orchestration & deployment:**
- Wave-based modernization for large estates
- Dependency mapping between applications and databases
- Human-in-the-loop (HITL) checkpoints at critical stages
- Amazon ECS and EC2 deployment with auto-scaling support
- Infrastructure-as-code generation (CloudFormation or CDK)
- Automated deployment validation with health checks and rollback
- CI/CD integration

### Requirements

Available in US East (N. Virginia) only.

- SQL Server 2008 R2 through 2022 (all editions: Express, Standard, Enterprise)
- Application must be on .NET 6+ (port with ATX .NET first if on .NET Framework)
- Entity Framework 6.3–6.5, EF Core 1.0–10.0, or ADO.NET (all versions)
- Source in supported platform: GitHub, GitHub Enterprise Server, GitLab.com, GitLab Self-Managed, Bitbucket Cloud, Bitbucket Data Center, Azure DevOps, Azure DevOps Server, or Amazon S3
- DB user needs VIEW DEFINITION and VIEW DATABASE STATE permissions
- DB passwords must use printable ASCII characters only (excluding `/`, `@`, `"`, and spaces)
- VPC containing the source SQL Server must have subnets in at least 2 different Availability Zones
- Applications must successfully build and run
- Database connections must be discoverable in source code

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
| ELIGIBLE | App on .NET 6+, SQL Server 2008 R2–2022, supported ORM, supported repo/S3 |
| ELIGIBLE AFTER .NET PORTING | App on .NET Framework (port with .NET agent first), DB otherwise qualifies |
| PARTIALLY ELIGIBLE | DB qualifies but has SSIS/SSRS/SSAS/CLR/Linked Servers that need manual remediation |
| NOT APPLICABLE | No SQL Server database, or database is already PostgreSQL/MySQL/other |
| NOT ELIGIBLE | VB.NET app, no source code, unsupported scenario |

### Two-Step Dependency

Only applies when the source application is on a .NET version older than the minimum supported by ATX SQL (currently .NET 6). In that case, ATX .NET must run first to port the application to .NET 6+, then ATX SQL can process the database modernization. If the application is already on .NET 6+, ATX SQL can be used directly without ATX .NET.

---

## CHECK 3 — ATX Custom & Kiro (Agentic Code Modernization)

Both ATX Custom and Kiro are agentic AI tools that can perform code modernization work. They serve different sweet spots depending on scale and repeatability. Additionally, ATX Custom now provides AWS-managed transformation definitions for common scenarios, and a Continuous Modernization capability for ongoing tech debt management.

Available in 8 regions: US East (N. Virginia), Europe (Frankfurt), Europe (London), Asia Pacific (Mumbai), Asia Pacific (Sydney), Asia Pacific (Tokyo), Asia Pacific (Seoul), Canada (Central).

### ATX Custom — Repeatable Transformations at Scale

ATX Custom is designed for scenarios where the same transformation pattern needs to be applied consistently across many codebases, components, or projects. It shines when you can define a transformation once and execute it repeatedly.

Key concepts:
- **Transformation definitions**: A skill package (SKILL.md + optional references/ and scripts/ folders) that describes a specific transformation. Stored in a per-account registry, versioned, and shareable across teams.
- **AWS-managed transformations**: Ready-to-use, AWS-vetted transformation definitions for common scenarios (Java version upgrades, Python upgrades, Node.js upgrades, AWS SDK migrations, Graviton migration, framework transitions). No definition phase needed — invoke directly from the CLI.
- **Before-and-after code samples**: ATX Custom works best when you can provide example input/output code. A practical approach is to modernize 1–2 applications first (e.g., using Kiro), then use the before vs. after code to define and teach the ATX Custom transformation definition.
- **Continual learning**: The agent automatically extracts lessons from every execution and developer feedback. These accumulate over time, improving transformation quality as more repos are processed. Lessons are transformation-specific and account-specific. Manage lessons with `atx custom def learnings`.
- **Bulk execution**: Supports non-interactive/headless mode (`-x` flag) for CI/CD pipeline integration, enabling batch processing across many repos without human intervention. Can be scaled via AWS Batch/Fargate.
- **Build/validation commands**: Optional but recommended — lets the agent validate transformed code during execution, feeding results back into the continual learning loop.
- **Client-side skills**: Additional capabilities that extend the agent during executions. Place at project-level for repo-specific standards or user-level for cross-project tools.
- **Draft vs published**: Iterative development workflow — create locally, save as draft for testing, refine, then publish to share with your team.

Best for:
- Repeatable patterns across 10+ codebases (e.g., replacing a deprecated internal library, upgrading a shared framework version, standardizing coding patterns)
- Organization-specific transformations (proprietary framework migrations, internal API upgrades)
- Supplementary transformations after ATX .NET / ATX SQL completes (e.g., upgrading remaining dependencies, refactoring patterns the main agents don't cover)
- Components the .NET/SQL agents cannot handle that follow a consistent pattern (CLR Assembly rewrites, complex T-SQL patterns)
- Common language/framework upgrades where AWS-managed transformations exist (Java 8→17, Python 3.9→3.13, Node.js 12→22, Spring Boot 2.x→3.x)

AWS-managed transformations (invoke directly, no definition phase):

| Transformation | Description |
|---------------|-------------|
| AWS/java-version-upgrade | Java 8→17, 11→21, etc. |
| AWS/python-version-upgrade | Python 3.9→3.13, Lambda runtime upgrades |
| AWS/nodejs-version-upgrade | Node.js 12→22, Lambda runtime upgrades |
| AWS/java-aws-sdk-v1-to-v2 | AWS SDK for Java v1→v2 |
| AWS/graviton-migration | x86→ARM/Graviton |
| + others | Check `atx custom def list` for full catalog |

Workflow:
1. **Pilot with Kiro** — Modernize 1–2 representative applications manually using Kiro to establish the before/after pattern
2. **Define transformation** — Use the pilot output as reference material to create an ATX Custom transformation definition (natural language instructions + before/after code samples + reference docs). Or use an AWS-managed transformation directly.
3. **Test and refine** — Run the transformation on a few more repos, review results, provide feedback. The continual learning system improves quality with each iteration.
4. **Scale execution** — Publish the transformation and execute across the remaining portfolio via CLI (interactive or headless via `atx custom def exec -t -n <name> -p <path>`)

CLI quick reference:
- `atx` — Start interactive conversation
- `atx custom def list` — List available transformations (AWS-managed + custom)
- `atx custom def exec -t -n <name> -p <path>` — Execute a transformation
- `atx custom def exec -t -n <name> -g file://<config.yaml> -p <path>` — Execute with config file (additionalPlanContext)
- `atx custom def publish -n <name> --sd <dir>` — Publish to registry
- `atx custom def learnings -n <name>` — Review/manage continual learning lessons

### ATX Continuous Modernization (Preview)

AWS Transform continuous modernization is a new capability that automatically identifies, prioritizes, and remediates tech debt across enterprise software portfolios. Available in US East (N. Virginia) and Europe (Frankfurt).

Key capabilities:
- **Rapid tech debt analysis**: fast metadata-only scans of package manifests (pom.xml, package.json, requirements.txt) to identify stale versions and outdated dependencies
- **Comprehensive tech debt analysis**: deep code-level analysis examining source code for debt patterns, code quality issues, architecture concerns
- **Security analysis**: vulnerability detection in source code and dependencies (via AWS Security Agent / AWS Continuum)
- **Agentic readiness**: assesses codebase readiness for AI agent integration
- **Modernization readiness**: evaluates candidates for containerization, serverless migration, platform upgrades
- **Custom analysis**: run your own transformation definitions as analysis (organization-specific policies)

Execution modes:
- **Local mode**: runs on your machine, good for trying out or small repos
- **Infrastructure mode**: runs on AWS with Fargate or EC2, supports teams, scheduling, and scale (deploy via CDK stack)

Sources: GitHub organizations, GitLab groups, Bitbucket workspaces, and local repositories.

Accessible from: AWS Transform web console, ATX CLI, Kiro Power, or agent plugins in other IDEs.

Best for:
- Post-modernization maintenance: keeping modernized applications current with latest dependency versions
- Portfolio-wide tech debt visibility before starting a modernization engagement
- Continuous security remediation at scale
- Scheduling recurring analyses (daily, weekly) with auto-remediation thresholds
- Assessing agentic readiness of codebases for AI-assisted development workflows

### Kiro — Per-Application Modernization

Kiro is an agentic coding IDE that can perform the same types of code transformations as ATX Custom, but is oriented toward interactive, per-application work. It excels at understanding application context, making nuanced decisions, and handling one-off or complex modernization tasks that don't follow a repeatable pattern.

Key capabilities for modernization:
- Specification-driven workflows (requirements → design → tasks) for structured modernization
- Agent hooks for automated validation (lint, test, security scan on each change)
- Steering documents for team-wide standards and modernization patterns
- MCP server integrations (AWS Knowledge, CAST Imaging, private registries, internal docs)
- AWS Transform Power for direct access to ATX Custom CLI from the IDE
- Powers ecosystem for extending capabilities (AWS Transform, CAST Imaging, etc.)

Best for:
- First 1–2 pilot applications (establishing the modernization pattern before scaling with ATX Custom)
- Complex, one-off modernization work where each application has unique challenges
- Architectural redesign (e.g., decomposing a monolith, replacing BizTalk with Step Functions)
- Post-transformation cleanup and refinement (addressing NextSteps.md items from ATX .NET)
- Applications where the developer needs to make judgment calls throughout the process
- Running ATX Custom transformations directly from the IDE via the AWS Transform Power

### ATX Comprehensive Codebase Analysis

The `AWS/comprehensive-codebase-analysis` managed transformation provides AI-powered deep static analysis of any codebase. It produces structured documentation that accelerates assessment phases and informs modernization planning.

**When to use:**
- Phase 200 (Feasibility): When the customer already has 1–few candidate applications identified and you want evidence-based complexity data before scoring
- Phase 300 (Detailed Assessment): As input to architecture decisions, questionnaire answers, and planning docs
- Front-loaded EBA engagements: Run before the EBA to give teams pre-built understanding of the application without weeks of manual code review
- Inherited or undocumented codebases where architecture knowledge is scattered or missing

**What it produces:**
- **Technical debt report**: prioritized view of outdated components, security issues by severity, maintenance concerns by long-term impact
- **Architectural documentation**: system-wide overview, component dependency graphs, service interaction patterns, data flow diagrams
- **Business logic extraction**: plain-language descriptions of complex processes, key business rules, validation logic, external integration points
- **Code analysis**: file-by-file breakdown with complexity metrics, code quality indicators, refactoring opportunities

**How to run:**

1. Copy the configuration file from `.kiro/skills/feasibility-analysis/references/atxconfig-coda.yaml` into the customer working folder (e.g., `working/{CUSTOMER}/200-mod-feasibility/atxconfig-coda.yaml`). Customize the `additionalPlanContext` if you need the analysis to focus on specific areas (e.g., data access patterns, auth usage, cross-database joins).
2. **Export database DDLs into the repository** — Before running the analysis, ask the customer to export all SQL Server DDL scripts (tables, views, stored procedures, functions, triggers) into a folder within the repository (e.g., `database/` or `sql/`). This gives the analysis agent full visibility into the data layer alongside the application code, producing more accurate dependency maps, stored procedure call graphs, and data access pattern identification. Use SSMS "Generate Scripts" or a tool like `mssql-scripter` to export:
   ```
   mssql-scripter -S <server> -d <database> -f ./database/ --file-per-object
   ```
   If exporting is not possible pre-engagement, the analysis will still run on application code alone — but the database architecture section of the output will be incomplete and require supplementing during Phase 300.
3. Execute from the directory containing the config file:

```
atx custom def exec -t -n AWS/comprehensive-codebase-analysis -g file://atxconfig-coda.yaml -p /path/to/repository
```

- `-t` — trusts all tool executions without prompting
- `-n` — the AWS-managed transformation name
- `-g` — path to the config YAML with `additionalPlanContext`
- `-p` — path to the repository being analyzed

The provided `atxconfig-coda.yaml` (in `.kiro/skills/feasibility-analysis/references/`) includes context to produce structured output with a navigable table of contents, functional requirements as user stories grouped into epics/features, Mermaid architecture diagrams (validated for syntax), and tech stack metrics including lines of code.

**Runtime:** minutes to hours depending on codebase size and complexity.

**How the output feeds into this framework:**
- Technical debt findings → refine complexity scoring in Step 3 (override base scores with evidence)
- Dependency graphs → inform cross-application dependency mapping in Step 4
- Architectural insights → pre-populate Phase 300 architecture working documents
- Business logic extraction → answer Phase 300 questionnaire items without customer interviews
- Component analysis → identify specific ATX blockers (3rd party UI, unsupported patterns) with precision

**Recommendation:** For engagements where the customer has already identified candidate applications (front-loaded EBA, assessment-only, or small portfolios < 5 apps), run the comprehensive codebase analysis on each candidate as the first action in Phase 200. Use the output as primary evidence for all subsequent feasibility scoring, pathway selection, and Phase 300 planning. This replaces weeks of manual code review and produces artefacts that can be shared with both technical teams and business stakeholders.

### When to Use Which

| Scenario | Recommended Tool |
|----------|-----------------|
| Same transformation pattern across many repos/projects | ATX Custom (define once, execute at scale) |
| First 1–2 pilot modernizations to establish the pattern | Kiro (then use output to define ATX Custom transformation) |
| One-off complex modernization with unique challenges | Kiro |
| Architectural redesign or replacement (no repeatable pattern) | Kiro |
| Post-ATX .NET / ATX SQL cleanup and refinement | Kiro |
| Organization-wide coding standard enforcement | ATX Custom |
| Batch library/dependency upgrades across portfolio | ATX Custom (AWS-managed transformations) |
| Common language version upgrades (Java, Python, Node.js) | ATX Custom (AWS-managed transformations) |
| Deep codebase understanding before modernization starts | ATX Comprehensive Codebase Analysis |
| Ongoing tech debt management post-modernization | ATX Continuous Modernization |
| Portfolio-wide tech debt visibility and security scanning | ATX Continuous Modernization |

### Supported Transformation Patterns (ATX Custom)

| Pattern | Complexity | Examples |
|---------|-----------|----------|
| API and Service Migrations | Medium | AWS SDK v1→v2 (Java, Python, JS), Boto2→Boto3, JUnit 4→5, javax→jakarta |
| Language Version Upgrades | Low-Medium | Java 8→17, Python 3.9→3.13, Node.js 12→22, TypeScript upgrades |
| Framework Upgrades | Medium | Spring Boot 2.x→3.x, React 17→18, Angular upgrades, Django upgrades |
| Framework Migrations | High | Angular→React, Redux→Zustand, Vue.js→React |
| Library and Dependency Upgrades | Low-Medium | Pandas 1.x→2.x, NumPy upgrades, Lodash upgrades |
| Code Refactoring | Low-Medium | Print→Logging, observability instrumentation, pattern modernization |
| Script Translations | Low-Medium | CDK→Terraform, Terraform→CloudFormation, Bash→PowerShell |
| Architecture Migrations | Medium-High | x86→Graviton, on-premises→Lambda, server→containers |
| Language-to-Language Migrations | Very High | Java→Python, JavaScript→TypeScript, C→Rust |
| Custom Transformations | Varies | Internal library migrations, proprietary framework upgrades |

---

## CHECK 4 — AWS Transform Containerization

AWS Transform includes an agentic AI capability to automate containerization of source code during migration. It analyzes applications, generates Docker images, publishes to ECR, and deploys to ECS or EKS with production-ready infrastructure code.

Available in all 8 ATX regions.

### Capabilities

- Analyzes source code to understand frameworks, dependencies, and runtime requirements
- Generates Dockerfiles and container images following AWS best practices
- Performs automated security scanning (vulnerability detection before publish)
- Publishes to Amazon ECR
- Generates IaC artifacts: Terraform modules (ECS) or Helm charts (EKS)
- Supports containerization as standalone or as part of a migration project
- Target platforms: Amazon ECS (Fargate or EC2) or Amazon EKS

### Source Code Support

- GitHub, GitLab, Bitbucket (via CodeConnections)
- .zip files (uploaded directly)

### When to Use

| Scenario | Value |
|----------|-------|
| .NET applications being modernized that need containerized deployment | Automates Dockerfile creation + IaC generation after ATX .NET completes |
| Migration project with mixed rehost + replatform workloads | Same project handles EC2 rehost and ECS/EKS replatform in parallel |
| Applications already on modern .NET needing cloud-native packaging | Standalone containerization without .NET porting |

### Classification

| Classification | Criteria |
|---------------|----------|
| APPLICABLE | Application source available, target is ECS or EKS, no Windows-specific runtime dependencies that prevent containerization |
| NOT APPLICABLE | COTS, no source code, desktop applications (WinForms/WPF), or target is EC2 Windows |

---

## CHECK 5 — AWS DMS & DMS Schema Conversion

AWS Database Migration Service (DMS) and its built-in Schema Conversion feature are the foundational database migration tools. They work independently of AWS Transform and cover scenarios where Transform for SQL Server is not applicable or where broader database migration flexibility is needed.

### DMS Schema Conversion (with Generative AI and Agentic Workflows)

Converts database schemas and code objects (stored procedures, functions, triggers, views, synonyms, data types) from source to target format. The GenAI feature uses Amazon Bedrock foundation models to automate conversion of complex code objects. The new agentic workflow orchestrates the entire lifecycle end-to-end via natural language in your IDE/terminal.

Supported source → target for this framework:
- SQL Server → Amazon Aurora PostgreSQL-Compatible
- SQL Server → Amazon RDS for PostgreSQL

**DMS SC with Generative AI:** Enhances the rule-based conversion engine by generating target SQL for objects the converter marks as action items. Toggle on per-project or per-object.

**DMS SC Agentic Workflows (New):** AI agents operate on top of DMS SC, orchestrating the full assessment and conversion lifecycle — create projects, browse metadata, run conversion, export reports, resolve action items — all within a single conversation from your IDE/terminal.

GenAI-assisted conversion available in 14 regions: US East (N. Virginia, Ohio), Asia Pacific (Tokyo, Osaka, Sydney), Canada (Central), Europe (Frankfurt, Zurich, Stockholm, Milan, Spain, Ireland, London, Paris).

Key capabilities:
- Automated schema and code object conversion with GenAI recommendations
- Migration assessment reports estimating complexity and identifying objects needing manual intervention
- Transformation rules for renaming schemas, tables, columns during conversion
- Extension packs for emulating source-specific features not natively available in PostgreSQL
- Agentic end-to-end workflow automation via natural language

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
| AWS Transform for SQL Server not eligible (VB.NET, no source code) but DB still needs migrating | DMS Schema Conversion + DMS Data Migration |
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

> **Note on ATX Comprehensive Codebase Analysis:** For engagements where candidate applications are already identified (front-loaded EBA, assessment-only, or small portfolios), run `AWS/comprehensive-codebase-analysis` on each candidate application before starting pathway execution. The output directly informs complexity scoring, pathway selection, architecture working documents, and Phase 300 questionnaire answers — replacing weeks of manual code review.

---

### Pathway 1 — ATX-Led Modernization

**When:** Application is eligible for ATX .NET and/or ATX SQL. This is the primary pathway for custom .NET applications with SQL Server databases. Covers three scenarios depending on what needs modernizing:
- App only (no SQL Server, or DB already on PostgreSQL/Aurora) → ATX .NET
- DB only (app already on .NET 6+) → ATX SQL
- Full-stack (app on .NET Framework + SQL Server) → ATX .NET first, then ATX SQL (two-step, only needed when app is below ATX SQL's minimum .NET version)

If the application has components ATX cannot handle (SSIS, SSRS, SSAS, CLR Assemblies, Linked Servers, etc.), use ATX for what it can handle and remediate the rest manually or with Kiro — see Manual Remediation Mapping below.

**Tools:** ATX .NET + ATX SQL + DMS (under the hood for data migration) + ATX Containerization (optional) + Kiro + ISV tools if needed

**Steps:**
1. (Optional) Run `AWS/comprehensive-codebase-analysis` to produce baseline architecture docs and tech debt report
2. ATX .NET agent: port .NET Framework → .NET 8/10 (web experience for supported repos/S3, IDE experience for TFS/SVN/local)
3. Review transformation summary and `NextSteps.md` — address remaining items using Kiro (Linux readiness, NuGet resolution, 3rd party UI component upgrades)
4. ATX SQL agent (if SQL Server → Aurora PostgreSQL): connect database, run assessment, review wave plan, execute schema conversion + data migration + app code updates
5. Review SQL agent flagged items — address using Kiro (complex T-SQL, stored procedure validation)
6. (Optional) ATX Containerization: generate Docker image + IaC for ECS/EKS deployment
7. Remediate unsupported components manually or with Kiro (SSRS → QuickSight, SSIS → Glue, CLR → rewrite, etc.)
8. Validate end-to-end, deploy through CI/CD pipeline

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
- An AWS-managed transformation exists for the scenario (Java, Python, Node.js upgrades, SDK migrations)

**Common scenarios:**
- F# or C++ projects that ATX .NET doesn't support, but need targeted refactoring or dependency upgrades
- Batch upgrades of internal libraries, NuGet packages, or coding patterns across many small projects
- Script translations (PowerShell → Bash, CDK → Terraform)
- Post-ATX cleanup patterns that repeat across many repos (e.g., standardizing logging, adding observability instrumentation)
- Java/Python/Node.js version upgrades using AWS-managed transformations

**Tools:** ATX Custom + Kiro (for pilot and edge cases)

**Steps:**
1. (Optional) Run `AWS/comprehensive-codebase-analysis` for deep understanding of the codebase before transformation
2. Pilot: manually modernize 1–2 representative files or projects using Kiro to establish the before/after pattern (skip if using AWS-managed transformation)
3. Define ATX Custom transformation using the pilot output, or select an AWS-managed transformation directly
4. Test on a few more repos, refine based on results (continual learning improves quality)
5. Scale execution across remaining targets via CLI (interactive or headless)
6. Kiro: address edge cases that the custom transformation doesn't handle

**Manual/Kiro work:** Kiro is essential for the pilot phase and for edge cases. After the transformation definition matures through a few iterations, manual effort decreases as the continual learning system improves quality.

---

### Pathway 3 — ATX .NET + DMS

**When:** .NET Framework application with SQL Server, but ATX SQL is not eligible (VB.NET app, DB not accessible from us-east-1, etc.) — or the decision is to keep SQL Server as the engine (migrate to RDS SQL Server instead of Aurora PostgreSQL).

**Tools:** ATX .NET agent + DMS Schema Conversion (GenAI + Agentic Workflows) + DMS Data Migration + Kiro

**Steps:**
1. (Optional) Run `AWS/comprehensive-codebase-analysis` for baseline architecture understanding
2. ATX .NET agent: port .NET Framework → .NET 8/10
3. Kiro: address post-transformation items
4. DMS Schema Conversion (with GenAI and agentic workflows): convert SQL Server schema → Aurora PostgreSQL / RDS PostgreSQL. Or use DMS homogeneous migration for SQL Server → RDS SQL Server.
5. DMS Data Migration: full load + CDC for near-zero downtime cutover
6. Kiro: update application connection strings and data access code (ATX SQL would have automated this, but it's not available in this pathway)
7. Validate end-to-end, deploy through CI/CD pipeline

**Manual/Kiro work:** Application data access code changes are manual since ATX SQL isn't handling them. DMS Schema Conversion GenAI handles the majority of schema conversion but expect manual review of complex stored procedures. The new agentic workflows reduce manual DMS console interaction.

---

### Pathway 4 — DMS Only (Database Migration)

**When:** Database migration without application code modernization. Covers two scenarios:
- **Heterogeneous** (SQL Server → Aurora PostgreSQL / RDS PostgreSQL): Use DMS Schema Conversion (GenAI + Agentic Workflows) + DMS Data Migration. For non-.NET apps, or when app code changes are handled separately.
- **Homogeneous** (SQL Server → RDS SQL Server): Use DMS homogeneous migration or native backup/restore. For replatforming to managed service while keeping the engine.

**Tools:** DMS Schema Conversion (GenAI) + DMS Data Migration, or native backup/restore

**Steps (heterogeneous):**
1. Create DMS migration project, run assessment report
2. Enable GenAI-assisted schema conversion (or use agentic workflow from IDE), review and address unconverted objects
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

**Tools:** Kiro (with AWS Transform Power installed for direct ATX Custom access)

**Steps:**
1. Run `AWS/comprehensive-codebase-analysis` to generate baseline documentation and architecture understanding
2. Open the application in Kiro
3. Use Kiro's spec-driven workflow (requirements → design → tasks) for structured modernization
4. Leverage steering documents and hooks for automated quality gates
5. Validate build, run tests, deploy through CI/CD pipeline

**Manual/Kiro work:** Kiro does the heavy lifting but the developer guides the process. Best for applications where context and judgment matter more than repeatability. The comprehensive codebase analysis output gives Kiro a head start by providing complete architecture context.

---

### Pathway 6 — Architectural Replacement

**When:** BizTalk, WCF Workflow Services, Windows Workflow Foundation (WF), legacy middleware, or other platforms with no direct port path. These require a ground-up redesign using AWS-native services.

**Tools:** Kiro + AWS services (Step Functions, EventBridge, Lambda, API Gateway, SQS, Amazon MQ)

**Steps:**
1. Run `AWS/comprehensive-codebase-analysis` to inventory all orchestrations, pipelines, workflows, and integration points
2. Design replacement architecture using AWS-native services (strangler fig pattern recommended)
3. Build replacement services using Kiro — use existing orchestration definitions and extracted business logic as specs
4. Migrate integrations one at a time, validate each before proceeding
5. Decommission legacy platform once all integrations are migrated

**Manual/Kiro work:** Most labor-intensive pathway. Kiro accelerates the build but architectural design and integration testing require significant developer effort. The comprehensive codebase analysis output is particularly valuable here as it extracts business logic in plain language from legacy orchestrations. For BizTalk, evaluate AWS B2B Data Interchange for EDI scenarios.

---

### Pathway 7 — Replatform (Lift-and-Shift)

**When:** COTS products (SAP, Tableau, etc.), no source code available, application retiring soon, or modernization not justified.

**Tools:** AWS Application Migration Service (MGN) + DMS + RDS + ATX Containerization (if source available and target is containers)

**Steps:**
1. AWS MGN: lift-and-shift application servers to EC2 Windows
2. DMS or native backup/restore: migrate SQL Server to RDS SQL Server
3. Configure Multi-AZ for HA, AWS Systems Manager for patching
4. Update DNS, validate, cut over

**Manual/Kiro work:** Minimal code changes — infrastructure migration only.

---

### Pathway 8 — Retain / Defer

**When:** SaaS replacement planned, low business priority, end-of-life, or modernization not justified at this time.

**Action:** Keep as-is, revisit in future assessment cycle. Document the rationale for deferral. Consider running ATX Continuous Modernization against the codebase to track tech debt growth and get alerts when dependencies reach end-of-life.

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
