---
name: start-project
description: Start a new modernization project. Creates the working folder structure, copies and customizes templates, determines project scope through natural conversation, and guides you on next steps.
---

## Workflow

Follow these steps in order to set up a new modernization project.

### Step 1 - Get project name

Ask for the project name if not already provided. This name will be used as the folder name and placeholder replacement throughout the project. Store it as `{PROJECT}`.

### Step 2 - Create folder structure

Create the full working directory structure for the project:

```
working/{PROJECT}/
  reference/
  100-data-collection/
  200-mod-feasibility/
  300-detailed-assessment/
    meetings/
  400-mod-plan/
```

Add `.gitkeep` files to empty folders: `reference/` and `300-detailed-assessment/meetings/`.

### Step 3 - Copy project template

Copy `references/project-template.md` to `working/{PROJECT}/engagement.md`.

Replace all `{PROJECT}` placeholders in the file with the actual project name.

### Step 4 - Copy blank inventory template

Copy `.kiro/skills/data-collection/references/100-App-Inventory.xlsx` to `working/{PROJECT}/100-data-collection/{PROJECT}-App-Inventory.xlsx`.

### Step 5 - Determine project scope

Ask natural questions to understand the scope of the modernization effort:

> To set things up correctly, tell me a bit about what you are looking to do:
>
> - **Do you have a portfolio of applications you want to modernize?**
>   We will start with a full inventory, feasibility scoring, and a prioritized wave plan before diving into specific apps.
>
> - **Do you have a specific application you want to modernize?**
>   We will skip the portfolio analysis and go straight into detailed assessment and planning for that application.
>
> - **Are you looking for an assessment and recommendations first?**
>   We will assess the landscape, complexity, and options without committing to execution plans yet.
>
> Which best describes your situation?

Wait for the answer before proceeding.

**Intent mapping:**
- "I have a portfolio of apps I want to modernize" or similar -> Full project (Phases 100 through 400)
- "I have a specific app I want to modernize" or similar -> Front-loaded, jump to Phase 300
- "I just want an assessment" or similar -> Assessment only (no Phase 400)

### Step 6 - Apply project scope to progress table

Update the Progress table in `working/{PROJECT}/engagement.md` based on the answer:

- **Portfolio modernization (Full):** Keep all phases as "Not started". Set engagement type field to "Full (100 to 400)".
- **Specific application (Front-loaded):** Mark Phase 100 and Phase 200 as `Skipped`. Set engagement type field to "Front-loaded EBA".
- **Assessment only:** Mark Phase 400 as `Skipped`. Set engagement type field to "Assessment only".

### Step 7 - Guide on next steps

Based on the project scope, explain which phase to start with and point to the relevant section in `working/{PROJECT}/engagement.md`:

- **Full / Assessment:** Start at Phase 100. Prepare or provide the application inventory spreadsheet. The `excel-analysis` skill can help work with the inventory.
- **Front-loaded (specific app):** Start at Phase 300. Gather application source code and consider running ATX Comprehensive Codebase Analysis. Phase 300 will begin with questionnaire completion.

Mention any reference materials to prepare (e.g., application inventory for Phase 100, source code access for Phase 300).
