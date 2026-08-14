# ISV Tool Guidance for .NET & SQL Server Modernization

This file provides guidance on when and how to use ISV (Independent Software Vendor) tools alongside AWS-native modernization tools. ISV tools are optional accelerators — they complement ATX and Kiro, not replace them.

## When to Consider ISV Tools

ISV tools add value in specific scenarios where AWS-native tools alone may not be sufficient:

- **Large portfolios (20+ applications):** Portfolio-level analysis, dependency mapping, and technical debt quantification help prioritize and plan before diving into per-app modernization.
- **Complex monolithic applications:** Applications that need decomposition into microservices before or during modernization benefit from runtime analysis and domain identification.
- **Unknown or undocumented architectures:** When architecture docs are missing or incomplete, automated code analysis tools can reverse-engineer the application structure.

---

## CAST Imaging

[CAST Imaging](https://www.castsoftware.com/) provides automated architecture analysis, dependency mapping, and technical debt quantification for complex software systems.

### Key Capabilities

- Reverse-engineers all interdependencies between code components and database structures across the full technology stack
- Generates interactive architecture visualizations from application level down to object-level details — tables, stored procedures, classes, methods, API endpoints, and their relationships
- Supports 50+ technologies including .NET Framework, .NET Core, C#, VB.NET, SQL Server, JavaScript, Java, and more
- Produces migration assessment reports that quantify complexity, identify blockers, and estimate effort
- Tracks architectural changes over time (compare versions, detect drift)
- MCP server integration (GA as of late 2025) — allows AI agents like Kiro to query CAST Imaging's architectural maps directly, giving the agent deterministic understanding of application structure rather than probabilistic inference from code alone

### When to Use

| Scenario | Value |
|----------|-------|
| Complex WebForms or MVC apps with extensive stored procedure logic (1000+ SPs) | Produces dependency maps showing which SPs are called from which code paths — critical for planning ATX SQL migration and identifying dead code |
| Applications with no architecture documentation | Reverse-engineers the actual architecture from source code, revealing hidden dependencies and integration points |
| Large portfolios needing prioritization | Quantifies technical debt per application, enabling data-driven wave planning |
| Pre-ATX assessment for high-risk applications | Identifies blockers, circular dependencies, and tightly coupled components before starting automated transformation |
| Monolith decomposition planning | Maps domain boundaries and data flows to inform microservices extraction |

### How It Fits in the Modernization Workflow

Use CAST Imaging during the **Assess phase** (Phase 200–300), before starting ATX transformation:

1. Onboard application source code and database schemas into CAST Imaging
2. Review generated architecture maps and dependency graphs
3. Use the assessment report to refine complexity scoring and identify blockers
4. Feed architectural insights into ATX transformation planning (or use the MCP server to give Kiro direct access to the maps during modernization)

### Licensing

CAST Imaging is licensed per application or per portfolio. Engage CAST directly for pricing. CAST is an AWS Partner with a listing on AWS Marketplace.

---

## vFunction

[vFunction](https://vfunction.com/) provides AI-driven architectural observability and automated refactoring for monolithic applications, helping decompose them into microservices.

### Key Capabilities

- **Architectural Observability:** Observes applications in production using a lightweight agent, analyzing runtime behavior to understand how domains, classes, and data flows are structured. Combines dynamic analysis (runtime) with static analysis (source code) for a complete picture.
- **Domain Identification:** Uses data science to automatically identify service boundaries and domain clusters within a monolith, based on actual runtime behavior rather than code structure alone.
- **Refactoring Engine:** Automates the creation and extraction of new microservices from Java and .NET monolithic applications. Generates new APIs, updates dependencies, and handles the mechanical refactoring work.
- **Technical Debt Quantification:** Measures architectural complexity, coupling, and cohesion metrics to quantify technical debt and track improvement over time.
- **Modernization Planning:** Generates actionable modernization plans with effort estimates, risk assessments, and recommended decomposition sequences.

### Supported Technologies

- Java (primary, most mature support)
- .NET / C# (supported for analysis and refactoring)
- Supports applications running on AWS (EKS, ECS, EC2, Lambda), Azure, and on-premises

### When to Use

| Scenario | Value |
|----------|-------|
| Large monolithic .NET applications (200K+ LOC) that need decomposition into microservices | Runtime analysis identifies actual service boundaries based on how the application behaves, not just how the code is structured |
| Applications where the team wants to move to EKS/ECS with proper service boundaries | vFunction identifies which components can be extracted as independent services and which are too tightly coupled |
| Portfolio-level architectural assessment | Analyzes multiple applications to identify shared dependencies, common patterns, and optimal decomposition strategies |
| Post-ATX modernization | After porting to modern .NET with ATX, use vFunction to plan and execute microservices decomposition if the target architecture calls for it |

### How It Fits in the Modernization Workflow

vFunction can be used at two points:

**Before ATX (decompose first, then port):**
1. Deploy vFunction agent to observe the monolith in production
2. Analyze domain boundaries and generate decomposition plan
3. Extract microservices from the monolith
4. Port each microservice individually using ATX .NET (smaller, simpler transformations)

**After ATX (port first, then decompose):**
1. Port the monolith to modern .NET using ATX .NET
2. Deploy to AWS (ECS/EKS)
3. Deploy vFunction agent to observe the modernized application
4. Decompose into microservices incrementally (strangler fig pattern)

The "port first" approach is generally simpler — get off .NET Framework first, then decompose. The "decompose first" approach is better when the monolith is too large or complex to port as a single unit.

### Licensing

vFunction is licensed per application with annual subscriptions. Engage vFunction directly for pricing. vFunction is an AWS Partner with an [AWS blog post](https://aws.amazon.com/blogs/apn/simplify-refactoring-monoliths-to-microservices-with-aws-and-vfunction/) describing the integration.

---

## Decision Matrix

Use this matrix to determine which ISV tools (if any) to recommend for a given portfolio:

| Portfolio Characteristic | Recommended ISV Tool | Rationale |
|-------------------------|---------------------|-----------|
| 20+ applications, limited architecture documentation | CAST Imaging | Portfolio-level visibility and dependency mapping before starting modernization |
| Complex app with 1000+ stored procedures or unknown dependencies | CAST Imaging | Dependency maps inform ATX SQL migration planning and identify dead code |
| Large monolith (200K+ LOC) needing microservices decomposition | vFunction | Runtime analysis identifies actual service boundaries |
| Small-medium portfolio (< 20 apps), well-documented, C# only | None needed | ATX + Kiro are sufficient |

---

## Notes

- ISV tools are accelerators, not requirements. For most portfolios with well-documented C# applications, ATX + Kiro are sufficient.
- ISV tool costs should be factored into the modernization business case. Engage vendors early for pricing and PoC options.
- Both CAST and vFunction are AWS Partners and have experience working alongside AWS modernization tools.
- CAST Imaging's MCP server integration is particularly valuable when using Kiro — it gives the AI agent deterministic architectural context for more accurate modernization decisions.
