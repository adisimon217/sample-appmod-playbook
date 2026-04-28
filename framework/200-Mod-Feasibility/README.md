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

## Output

```
working/{CUSTOMER}/200-mod-feasibility/
  {CUSTOMER}-Feasibility-Assessment.md
```

See [sample-output/analysis-output.md](sample-output/analysis-output.md) for an example.

Review the output, then proceed to [Phase 300](../300-Detailed-Assessment/README.md) for the apps selected for modernization.
