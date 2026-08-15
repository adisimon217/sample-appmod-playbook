---
inclusion: always
---

# ModNet Framework — Windows (.NET & SQL Server) Modernization on AWS

Structured modernization framework for AWS partners. Skills contain their own reference material (templates, rules, samples) in their `references/` subfolder. All customer-specific outputs go in `working/`.

## Skills

| Skill | Phase | References |
|-------|-------|------------|
| `start-engagement` | Setup | `references/engagement-template.md` |
| `data-collection` | 100 | `references/100-App-Inventory.xlsx` |
| `feasibility-analysis` | 200 | `references/feasibility-analysis.md`, `references/aws-tools-analysis.md`, `references/atxconfig-coda.yaml` |
| `detailed-assessment` | 300 | `references/300-App-Questionnaire.md`, `references/detailed-analysis.md`, `references/300-Architecture.md` |
| `generate-plans` | 400 | `references/PoC-Plan-Template.md`, `references/EBA-Plan-Template.md`, `references/EBA-Preparation-Checklist.md` |
| `excel-analysis` | Utility | (no references) |

Each skill's `references/` folder contains the templates, analysis rules, and sample outputs it needs. Skills reference their own files with relative paths (e.g., `references/feasibility-analysis.md`). When a skill needs another skill's reference file, it uses the full workspace path (e.g., `.kiro/skills/feasibility-analysis/references/atxconfig-coda.yaml`).

## Workspace Structure

```
.kiro/skills/                     # Skills with embedded reference material
  start-engagement/
    SKILL.md
    references/
  data-collection/
    SKILL.md
    references/
  feasibility-analysis/
    SKILL.md
    references/
  detailed-assessment/
    SKILL.md
    references/
  generate-plans/
    SKILL.md
    references/
  excel-analysis/
    SKILL.md

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

- Skill reference files (`.kiro/skills/*/references/`) are read-only reference material - never modified during customer work
- Each customer engagement starts by invoking the `start-engagement` skill
- The `engagement.md` in each customer folder is the primary interface - all prompts with paths pre-filled
- Phase 200 prompts read the Excel inventory directly (excel-analysis skill) - no CSV export
- All modernization plans should be decisive: one recommended approach per decision point
