---
inclusion: always
---

# Workspace Rules

## Framework protection

Files under `framework/` are read-only reference material. Never modify, delete, or overwrite files in the `framework/` folder during customer work. Always read from `framework/` and write to `working/`.

When prompts reference framework files (templates, analysis rules, samples), use them as inputs but produce outputs only in the `working/{CUSTOMER}/` folder.

## Working folder conventions

All customer-specific work goes in `working/{CUSTOMER}/`. The engagement.md file in each customer folder is the primary interface — it contains all prompts with paths pre-filled for that customer.

## Customer engagement initiation

When creating a new customer engagement:
1. Copy `framework/engagement-template.md` → `working/{CUSTOMER}/engagement.md`, replace `{CUSTOMER}` placeholders
2. For each phase (100, 200, 300, 400), copy the framework README into the customer folder:
   - `framework/100-Data-Collection/README.md` → `working/{CUSTOMER}/100-data-collection/README.md`
   - `framework/200-Mod-Feasibility/README.md` → `working/{CUSTOMER}/200-mod-feasibility/README.md`
   - `framework/300-Detailed-Assessment/README.md` → `working/{CUSTOMER}/300-detailed-assessment/README.md`
   - `framework/400-Mod-Plan/README.md` → `working/{CUSTOMER}/400-mod-plan/README.md`
3. In each copied README, replace `{CUSTOMER}` with the actual customer name
4. In each copied README, adjust relative links to framework artefacts — any link like `(300-App-Questionnaire.md)` or `(feasibility-analysis.md)` must be updated to point back to the framework location using a relative path from the customer folder (e.g., `(../../../framework/300-Detailed-Assessment/300-App-Questionnaire.md)`)
5. Links using `#File:framework/...` or `#Folder:working/{CUSTOMER}/...` syntax are already absolute and don't need adjustment
6. Copy the blank inventory template: `framework/100-Data-Collection/100-App-Inventory.xlsx` → `working/{CUSTOMER}/100-data-collection/{CUSTOMER}-App-Inventory.xlsx`
7. Scaffold empty subfolders with .gitkeep: `reference/`, `300-detailed-assessment/meetings/`
8. Ask the engagement type and mark skipped phases accordingly

Output paths follow this pattern:
- `working/{CUSTOMER}/100-data-collection/` — inventory
- `working/{CUSTOMER}/200-mod-feasibility/` — feasibility assessment
- `working/{CUSTOMER}/300-detailed-assessment/` — questionnaires, architecture docs, consolidated assessment, meetings
- `working/{CUSTOMER}/400-mod-plan/` — PoC plans, EBA plans, project plans
- `working/{CUSTOMER}/reference/` — customer-provided artefacts (read-only input)

## .gitkeep cleanup

When writing a real file to a folder that contains a .gitkeep, remove the .gitkeep.