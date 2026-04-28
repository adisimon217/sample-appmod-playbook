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
- Each customer engagement starts by running the initiation prompt in the root README
- The `engagement.md` in each customer folder is the primary interface — all prompts with paths pre-filled
- Phase 200 prompts read the Excel inventory directly (excel-analysis skill) — no CSV export
- All modernization plans should be decisive: one recommended approach per decision point

## Engagement Initiation

When asked to create a new engagement for a customer, follow these steps:

1. Read the engagement template from `framework/engagement-template.md`.
2. Copy it to `working/{CUSTOMER}/engagement.md`, replacing all `{CUSTOMER}` placeholders with the actual customer name.
3. Create the full folder structure:
   ```
   working/{CUSTOMER}/
     engagement.md
     reference/
     100-data-collection/
     200-mod-feasibility/
     300-detailed-assessment/
       meetings/
     400-mod-plan/
   ```
4. Ask the user for the engagement type:
   - Full (100→400): keep all phases in engagement.md
   - Front-loaded EBA: mark Phase 100 and 200 as ⏭️ Skipped
   - Assessment only: mark Phase 400 as ⏭️ Skipped
5. Apply the engagement type to the Progress table in engagement.md.
6. Guide the user on next steps — explain which phase to start with based on the engagement type, point them to the relevant prompt in `engagement.md`, and mention any reference materials they should prepare (e.g., application inventory for Phase 100).