---
inclusion: fileMatch
fileMatchPattern: "{.kiro/skills/detailed-assessment/references/**,.kiro/skills/generate-plans/references/**,working/**}"
---

# Assessment approach

There is typically one window to conduct the assessment that leads into modernization. The questionnaire must be complete enough but not overwhelming - aim for ~85 questions, 70:30 application vs database ratio. The assessment output must be sufficient to produce a solid scope and execution plan.

# Execution sequencing

Modernization plans follow a natural sequence: PoC → EBA → Full project. Not every app needs all three - the Phase 300 assessment determines the right entry point per app.

- **PoC (420)** comes first when there are high-risk unknowns (e.g., gMSA auth, cross-DB joins, native library compat). A PoC is 1-2 weeks, validates a specific hypothesis, and produces a go/no-go decision. PoC results feed back into the architecture working doc (🟡 → 🟢 or fallback).
- **EBA (430)** is the primary execution vehicle. 6-week engagement, 2-day hackathon. Suitable for apps < 100K LoC, ATX eligible, isolated, manageable complexity. PoC results (if any) are prerequisites - they must complete before the EBA starts.
- **Full modernization project** is for apps that exceed EBA scope (> 100K LoC, heavy UI rewrite, ASMX with many consumers, 0% test coverage). These are scoped as dedicated projects with phased delivery. Patterns established in the EBA pilot inform the approach.

When generating plans, produce all applicable plan types in one pass - a project typically includes a mix of PoCs, EBAs, and full projects across the portfolio.

# Supplementary tools selection

Prioritize AWS tools:
- [AWS Transform Custom - Comprehensive Codebase Analysis](https://docs.aws.amazon.com/transform/latest/userguide/transform-aws-customs.html) (`AWS/comprehensive-codebase-analysis`) - deep static analysis of legacy codebases. Generates structured documentation: architecture, technical debt, business logic extraction, code metrics. Run pre-EBA on any application with poor documentation, no tests, or complex undocumented dependencies. Especially valuable for large codebases (50K+ LoC), applications with multiple integration points, or when the team inheriting the code did not write it. Output informs EBA scope, sprint planning, and tooling decisions. Use `additionalPlanContext` in the config YAML to focus the analysis on modernization-relevant areas (e.g., data access patterns, auth usage, cross-database joins).
- [AWS Transform for .NET](https://docs.aws.amazon.com/transform/latest/userguide/dotnet.html) - assessment and code transformation (.NET Framework → cross-platform .NET)
- [AWS Transform for SQL Server](https://docs.aws.amazon.com/transform/latest/userguide/sql-server-modernization.html) - full-stack DB modernization
- [AWS Transform Custom](https://docs.aws.amazon.com/transform/latest/userguide/custom.html) - repeatable patterns at scale (ASMX → REST, custom transformations)
- [Kiro](https://kiro.dev/docs/) - complex rewrites, post-Transform cleanup, architectural work

Avoid decommissioned tools: App2Container, Porting Assistant for .NET, Microservice Extractor, Toolkit for .NET Refactoring.

# Database migration tooling selection

- **Kiro only:** app is a full rewrite, DB < 50 GB, simple schema, flexible cutover
- **SCT + DMS:** app is being ported, DB 50 GB-1 TB, moderate schema, or need CDC
- **ATX SQL:** app is being ported with existing .NET data access layer (EF6, ADO.NET)
- **SCT + DMS + Kiro:** DB > 50 GB with complex schema, tight cutover

Key thresholds: < 50 GB → scripted migration viable. > 50 GB → DMS. Need CDC → DMS regardless. Full rewrite → ATX SQL adds no value.

See detailed-analysis.md Section 12 for the full decision matrix.

# Plan decisiveness

Be decisive - one recommended approach per decision point, not a menu of options. At most 2 alternatives only when the choice genuinely depends on unconfirmed input. Examples:
- ORM: pick EF Core (not "EF Core or Dapper")
- Frontend: ASP.NET Core MVC for server-rendered, SPA (React) + Web API for interactive. Avoid Blazor unless necessary.
- Web Forms UI: Default recommendation is ATX .NET (automated port to Blazor) - propose this first and validate during architecture discussion. If Blazor is declined, fall back to Kiro + AI-DLC rewrite to ASP.NET Core MVC. Use ATX Custom for ASMX → REST API transformations.
- Use the project's actual name, not generic placeholders

# Execution prerequisites

- **Comprehensive codebase analysis:** For applications with poor documentation, no tests, or complex undocumented dependencies (especially 50K+ LoC), run `AWS/comprehensive-codebase-analysis` via AWS Transform Custom 2-3 weeks before the EBA. The output (architecture docs, technical debt report, business logic extraction, code metrics) replaces months of manual code review and directly informs EBA scope, sprint planning, and tooling decisions. Share results with engineering (tech debt → sprint planning), AWS team (architecture → scope refinement), and stakeholders (business logic → workflow validation).
- **Sandbox environment:** The full app stack must be loadable in isolation (local laptop, shared dev VM, or cloud dev env) before the EBA. Dependencies must be accessible or mocked. This cannot be figured out during the 2-day.
- **Functional spec for rewrites:** When the approach is a full rewrite (not port), develop a functional spec with user stories in weeks 1-2. Cover only EBA-scoped workflows. Derive from analyzing legacy code with Kiro + AI-DLC (supplement with codebase analysis output if available). Validate before development begins.
- **Dev environment (Windows → Linux containers):** Include setup instructions in the plan. WSL 2, Docker Desktop (WSL 2 backend), .NET LTS SDK, `core.autocrlf=input`, `.gitattributes` with `eol=lf`, clone repos in WSL filesystem not `/mnt/c/`. Pre-EBA validation checklist required.
- **Email:** Mock for EBA (log to console/CloudWatch). Abstract behind a service interface for post-EBA swap to SES or SMTP.
- **DB auth:** Prefer IAM database authentication for Aurora PostgreSQL over stored credentials.
- **File uploads:** S3 multipart upload for EBA. Full file storage architecture (tus, S3 File Gateway) as post-EBA follow-up.

# Execution methodology

Two methodologies, often combined:
1. [AWS AI-DLC](https://github.com/awslabs/aidlc-workflows) - AI-Driven Development Life Cycle
2. [ModAx EBA](https://aws.amazon.com/experience-based-acceleration/) - 6-week engagement, 2-day hackathon

Target: deploy at least one pilot workload on AWS modern services (.NET 10 on ECS Fargate Linux + Aurora PostgreSQL). Stretch: CI/CD (CodePipeline), Observability (OpenTelemetry + CloudWatch).
