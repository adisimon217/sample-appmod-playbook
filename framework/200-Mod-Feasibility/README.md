# Phase 200 — Modernization Feasibility

Analyze the Phase 100 inventory to classify workloads, score complexity, assess tool eligibility, and produce a prioritized wave plan.

## How to Run

Set `{CUSTOMER}` and run the prompt. Analysis rules, scoring, and output format are in `feasibility-analysis.md`.

```
Read the inventory at #File:working/{CUSTOMER}/100-data-collection/{CUSTOMER}-App-Inventory.xlsx
(all sheets — Applications and Databases).

Then read #File:framework/200-Mod-Feasibility/feasibility-analysis.md and execute all four steps.

Save as working/{CUSTOMER}/200-mod-feasibility/{CUSTOMER}-Feasibility-Assessment.md.
```

## Accelerator: ATX Comprehensive Codebase Analysis

For engagements where the customer already has candidate applications identified (front-loaded EBA, assessment-only, or small portfolios ≤ 5 apps), run the ATX comprehensive codebase analysis on each candidate before or alongside this phase. This replaces weeks of manual code review and produces evidence-based input for complexity scoring, pathway selection, and Phase 300 planning.

```
Run ATX comprehensive codebase analysis against the following application repositories:
- /path/to/app-1
- /path/to/app-2

Prerequisites:
  Ask the customer to export all SQL Server DDLs (tables, views, stored procedures, functions,
  triggers) into a folder within each repository (e.g., database/ or sql/) before running.
  Use SSMS "Generate Scripts" or mssql-scripter:
    mssql-scripter -S <server> -d <database> -f ./database/ --file-per-object
  This gives the analysis full visibility into the data layer alongside application code.

First, copy the config file:
  Copy framework/200-Mod-Feasibility/assets/atxconfig-coda.yaml
  to working/{CUSTOMER}/200-mod-feasibility/atxconfig-coda.yaml

For each application, execute from the working/{CUSTOMER}/200-mod-feasibility/ directory:
  atx custom def exec -t -n AWS/comprehensive-codebase-analysis -g file://atxconfig-coda.yaml -p <repo-path>

Save the output for each application in working/{CUSTOMER}/200-mod-feasibility/{APP-NAME}-codebase-analysis/

Then run the standard feasibility analysis prompt above, using the codebase analysis output
as primary evidence for complexity scoring and pathway selection.
```

When codebase analysis output is available, the feasibility analysis will use measured metrics (actual LOC, actual dependency counts, specific blocker identification) instead of inventory-level estimates, producing higher-confidence scoring.

## Output

```
working/{CUSTOMER}/200-mod-feasibility/
  {CUSTOMER}-Feasibility-Assessment.md
  {APP-NAME}-codebase-analysis/          (if accelerator used)
```

See [sample-output/analysis-output.md](sample-output/analysis-output.md) for an example.

Review the output, then proceed to [Phase 300](../300-Detailed-Assessment/README.md) for the apps selected for modernization.
