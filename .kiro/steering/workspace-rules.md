---
inclusion: always
---

# Workspace Rules

## Skill references protection

Files under `.kiro/skills/*/references/` are read-only reference material. Never modify, delete, or overwrite files in any skill's `references/` folder during project work. Always read from `references/` and write to `working/`.

When skills reference templates, analysis rules, or samples from their `references/` folder, use them as inputs but produce outputs only in the `working/{PROJECT}/` folder.

## Working folder conventions

All project-specific work goes in `working/{PROJECT}/`. The engagement.md file in each project folder is the primary interface - it contains progress tracking with paths pre-filled for that project.

Output paths follow this pattern:
- `working/{PROJECT}/100-data-collection/` - inventory
- `working/{PROJECT}/200-mod-feasibility/` - feasibility assessment
- `working/{PROJECT}/300-detailed-assessment/` - questionnaires, architecture docs, consolidated assessment, meetings
- `working/{PROJECT}/400-mod-plan/` - PoC plans, EBA plans, project plans
- `working/{PROJECT}/reference/` - reference artefacts (read-only input)

## .gitkeep cleanup

When writing a real file to a folder that contains a .gitkeep, remove the .gitkeep.
