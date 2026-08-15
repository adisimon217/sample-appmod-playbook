---
name: feasibility-analysis
description: Execute Phase 200 modernization feasibility analysis. Reads the completed Phase 100 inventory, classifies Windows workloads, checks AWS Transform eligibility, scores technical complexity, and produces a prioritized wave plan with pilot/PoC selection. Optionally runs ATX Comprehensive Codebase Analysis for evidence-based scoring.
---

## Workflow

Follow these steps in order to complete Phase 200 feasibility analysis for a customer engagement.

### Step 1 - Identify the customer and verify prerequisites

Determine the customer name (`{CUSTOMER}`) from context. Verify that:

1. The validated inventory exists at `working/{CUSTOMER}/100-data-collection/{CUSTOMER}-App-Inventory.xlsx`
2. The working folder `working/{CUSTOMER}/200-mod-feasibility/` exists

If the inventory does not exist or Phase 100 is not complete, ask the user to invoke the `data-collection` skill first.

### Step 2 - Read the inventory

Use the `excel-analysis` skill to read all sheets from `working/{CUSTOMER}/100-data-collection/{CUSTOMER}-App-Inventory.xlsx`:

- **Applications sheet**: one row per app with 22 columns (App ID, App Name, Application Type, .NET Version, LOC, Windows Dependencies, IIS Dependencies, etc.)
- **Databases sheet**: one row per database with 19 columns (DB ID, Linked App ID, SQL Server version, stored procedure count, advanced features, etc.)
- **Metadata sheet**: customer name, date, assessor

Display a summary of the portfolio (number of applications, number of databases, key statistics).

### Step 3 - Ask about the ATX Comprehensive Codebase Analysis accelerator

For engagements where the customer already has candidate applications identified (front-loaded EBA, assessment-only, or small portfolios with 5 or fewer apps), recommend running the ATX Comprehensive Codebase Analysis:

> **Optional Accelerator: ATX Comprehensive Codebase Analysis**
>
> For this portfolio size, I recommend running the ATX Comprehensive Codebase Analysis on each candidate application before scoring. This produces evidence-based metrics (actual LOC, actual dependency counts, specific blocker identification) instead of inventory-level estimates.
>
> **Prerequisites:**
> - Application source code must be accessible
> - Ask the customer to export all SQL Server DDLs (tables, views, stored procedures, functions, triggers) into a folder within each repository (e.g., `database/` or `sql/`). Use SSMS "Generate Scripts" or:
>   ```
>   mssql-scripter -S <server> -d <database> -f ./database/ --file-per-object
>   ```
>
> **To run:**
> 1. Copy `references/atxconfig-coda.yaml` to `working/{CUSTOMER}/200-mod-feasibility/atxconfig-coda.yaml`
> 2. For each application, execute from `working/{CUSTOMER}/200-mod-feasibility/`:
>    ```
>    atx custom def exec -t -n AWS/comprehensive-codebase-analysis -g file://atxconfig-coda.yaml -p <repo-path>
>    ```
> 3. Save output in `working/{CUSTOMER}/200-mod-feasibility/{APP-NAME}-codebase-analysis/`
>
> Would you like to run the codebase analysis accelerator, or proceed with inventory-based scoring?

If the user chooses to run the accelerator, guide them through the steps above. Once output is available, use it as primary evidence for complexity scoring in Step 5.

### Step 4 - Execute the feasibility analysis

Read the analysis rules from `references/feasibility-analysis.md` and the AWS tool eligibility checks from `references/aws-tools-analysis.md`.

Execute the analysis in four steps as defined in `feasibility-analysis.md`:

#### Step 4a - Identify Windows workloads

Classify each application into one of four categories:

- **CAT1 - WINDOWS-BOUND**: Tied to Windows (Windows-only app type, .NET Framework < 5, Windows/IIS dependencies, VB/C++/F#)
- **CAT2 - LIKELY LINUX-READY BUT MAY STILL BE ON WINDOWS**: Modern .NET (5+) on premises/Elastic Beanstalk, no containers or Docker/ECS only
- **CAT3 - LIKELY LINUX-READY**: Already on Kubernetes/OpenShift with no Windows/IIS dependencies
- **CAT4 - UNCLEAR - NEEDS VERIFICATION**: Insufficient data or ambiguous indicators

Only CAT1 and CAT2 applications proceed to the next steps. CAT3 applications are flagged but do not need modernization. CAT4 applications need data clarification.

Present the classification table to the user for review.

#### Step 4b - AWS Transform eligibility and modernization pathways

For each CAT1 and CAT2 application, check eligibility against the rules in `references/aws-tools-analysis.md`:

- **CHECK 1 - ATX .NET**: GA eligible, Preview eligible, IDE only, or Not eligible
- **CHECK 2 - ATX SQL**: Eligible, Eligible after .NET porting, Partially eligible, Not applicable, or Not eligible
- **CHECK 3 - ATX Custom / Kiro**: Identify which tool is appropriate (repeatable pattern vs one-off)
- **CHECK 4 - ATX Containerization**: Applicable or Not applicable
- **CHECK 5 - DMS / DMS Schema Conversion**: For database migration flexibility

If the AWS Knowledge MCP Server is available, verify the latest AWS Transform capabilities by searching for current eligibility criteria. Override the static rules where the latest documentation differs.

If the AWS Knowledge MCP Server is not available, proceed with the static rules in `references/aws-tools-analysis.md` as-is and note to the user that eligibility criteria should be verified against current AWS documentation before finalizing recommendations.

Present the eligibility matrix to the user.

#### Step 4c - Technical complexity scoring

Score each Windows-bound application using the methodology in `feasibility-analysis.md` Step 3:

- **Application Complexity (0-10)**: Score based on app type, .NET version, codebase size, dependencies, source availability, and language. Apply tool-aware adjustments (ATX eligibility reduces complexity).
- **Database Complexity (0-10)**: Score based on data store type, stored procedures, triggers, advanced features, component services, HA setup, DB size, and access method. Apply tool-aware adjustments.
- **Combined Technical Complexity**: Average of App and DB scores, rounded to nearest integer.

If ATX Comprehensive Codebase Analysis output is available, use measured metrics to override base scores:
- Replace LOC estimates with actual LOC from code analysis
- Replace dependency guesses with actual dependency counts
- Identify specific ATX blockers (3rd party UI components, unsupported patterns) with precision

Present the scoring table with base and adjusted scores, plus the top complexity drivers for each application.

#### Step 4d - Prioritization and wave planning

Calculate Business Value scores (0-10) based on criticality, change frequency, user count, and RTO/RPO sensitivity.

Plot applications on the prioritization matrix:
- **Quick Wins** (High Value, Low Complexity): Prioritize for immediate modernization
- **Strategic** (High Value, High Complexity): Detailed planning, break into phases
- **Selective** (Low Value, High Complexity): Consider retire, isolate, or replace
- **Low-Priority** (Low Value, Low Complexity): Batch modernize, minimal resources

Generate the Mermaid quadrant chart using the coordinate mapping from the analysis rules.

Select the Pilot/PoC from the Quick Wins quadrant (mandatory). Criteria:
1. Must be in Quick Wins quadrant (non-negotiable)
2. GA eligible for ATX .NET, C#, source in Git, < 100K LOC
3. Include at least one app with SQL Server database
4. Prefer fewer upstream/downstream dependencies

Assign waves: Pilot -> Wave 1 (Quick Wins) -> Wave 2 (Strategic) -> Wave 3 (Low-Priority) -> Selective (case-by-case).

Flag data gaps needed for Phase 300 per application.

### Step 5 - Save the output

Save the complete feasibility assessment as:
`working/{CUSTOMER}/200-mod-feasibility/{CUSTOMER}-Feasibility-Assessment.md`

The output should contain four sections corresponding to Steps 4a-4d:
1. Windows Workload Classification
2. AWS Transform Eligibility Matrix
3. Technical Complexity Scoring
4. Prioritization Matrix and Wave Plan (including the Mermaid quadrant chart)

If ATX Codebase Analysis was used, note "ATX Codebase Analysis available" per application and reference specific findings.

### Step 6 - Mark Phase 200 complete and proceed

1. Update the Progress table in `working/{CUSTOMER}/engagement.md` to mark Phase 200 as "Complete".
2. Present a summary of the key findings:
   - Total applications assessed
   - Pilot/PoC selection with rationale
   - Wave plan overview
   - Key data gaps to resolve in Phase 300
3. Recommend next steps:

> Phase 200 Modernization Feasibility is complete. The assessment is saved at:
> `working/{CUSTOMER}/200-mod-feasibility/{CUSTOMER}-Feasibility-Assessment.md`
>
> **Next steps:**
> - Review the feasibility assessment with the customer
> - For the Pilot/PoC application(s), gather source code access and prepare for detailed assessment
> - Invoke the Phase 300 skill to begin detailed assessment and questionnaire completion for the selected applications
>
> The applications selected for modernization in the wave plan should proceed to Phase 300 in wave order (Pilot first, then Wave 1, Wave 2, etc.).
