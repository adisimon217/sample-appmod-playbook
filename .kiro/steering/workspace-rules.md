---
inclusion: always
---

# Workspace Rules

## Skill references protection

Files under `.kiro/skills/*/references/` are read-only reference material. Never modify, delete, or overwrite files in any skill's `references/` folder during customer work. Always read from `references/` and write to `working/`.

When skills reference templates, analysis rules, or samples from their `references/` folder, use them as inputs but produce outputs only in the `working/{CUSTOMER}/` folder.

## Working folder conventions

All customer-specific work goes in `working/{CUSTOMER}/`. The engagement.md file in each customer folder is the primary interface — it contains all prompts with paths pre-filled for that customer.

Output paths follow this pattern:
- `working/{CUSTOMER}/100-data-collection/` — inventory
- `working/{CUSTOMER}/200-mod-feasibility/` — feasibility assessment
- `working/{CUSTOMER}/300-detailed-assessment/` — questionnaires, architecture docs, consolidated assessment, meetings
- `working/{CUSTOMER}/400-mod-plan/` — PoC plans, EBA plans, project plans
- `working/{CUSTOMER}/reference/` — customer-provided artefacts (read-only input)

## .gitkeep cleanup

When writing a real file to a folder that contains a .gitkeep, remove the .gitkeep.
