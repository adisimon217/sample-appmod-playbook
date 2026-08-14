---
inclusion: always
---

# ModNet Framework — Windows (.NET & SQL Server) Modernization on AWS

Structured modernization framework for AWS partners. Two top-level folders:

- `framework/` — read-only reference material (templates, rules, samples). Never modified per-customer.
- `working/` — per-customer work. All outputs go here.

## Framework Phases

| Phase | What happens | Framework docs |
|-------|-------------|---------------|
| 100 — Data Collection | Portfolio inventory | `framework/100-Data-Collection/` |
| 200 — Mod Feasibility | Classify, score, prioritize, wave plan | `framework/200-Mod-Feasibility/` |
| 300 — Detailed Assessment & Architecture | Per-app questionnaire, collaborative architecture, consolidated assessment | `framework/300-Detailed-Assessment/` |
| 400 — Mod Plan | PoC plans, EBA plans, full project plans | `framework/400-Mod-Plan/` |

## Workspace Structure

```
framework/                        # Read-only. Templates, rules, samples.
  100-Data-Collection/
  200-Mod-Feasibility/
    210-Tool-Feasibility/
  300-Detailed-Assessment/
  400-Mod-Plan/
    410-Accelerators/
    420-PoC/
    430-EBA/
  engagement-template.md

working/                          # Per-customer work.
  {CUSTOMER}/
    engagement.md                 # Runbook with all prompts
    reference/
    100-data-collection/
    200-mod-feasibility/
    300-detailed-assessment/
      meetings/
    400-mod-plan/

.references/                      # Source reference frameworks (OMS, MODA, MVA)
```

## Key Conventions

- `framework/` is never modified during customer work — read from it, write to `working/`
- Each customer engagement starts by invoking the `start-engagement` skill
- The `engagement.md` in each customer folder is the primary interface — all prompts with paths pre-filled
- Phase 200 prompts read the Excel inventory directly (excel-analysis skill) — no CSV export
- All modernization plans should be decisive: one recommended approach per decision point

