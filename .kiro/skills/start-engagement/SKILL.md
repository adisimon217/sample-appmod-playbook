---
name: start-engagement
description: Start a new customer modernization engagement. Creates the working folder structure, copies and customizes templates, determines engagement type (Full, Front-loaded EBA, or Assessment only), and guides the user on next steps.
---

## Workflow

Follow these steps in order to set up a new customer engagement.

### Step 1 - Get customer name

Ask the user for the customer name if not already provided. This name will be used as the folder name and placeholder replacement throughout the engagement. Store it as `{CUSTOMER}`.

### Step 2 - Create folder structure

Create the full working directory structure for the customer:

```
working/{CUSTOMER}/
  reference/
  100-data-collection/
  200-mod-feasibility/
  300-detailed-assessment/
    meetings/
  400-mod-plan/
```

Add `.gitkeep` files to empty folders: `reference/` and `300-detailed-assessment/meetings/`.

### Step 3 - Copy engagement template

Copy `references/engagement-template.md` to `working/{CUSTOMER}/engagement.md`.

Replace all `{CUSTOMER}` placeholders in the file with the actual customer name.

### Step 4 - Copy blank inventory template

Copy `.kiro/skills/data-collection/references/100-App-Inventory.xlsx` to `working/{CUSTOMER}/100-data-collection/{CUSTOMER}-App-Inventory.xlsx`.

### Step 5 - Ask engagement type

Present the user with the following engagement type question:

> To set up this engagement correctly, I need to understand where you are starting from:
>
> **A) Customer already has 1-2 candidate applications in mind** - they know which apps to modernize and want to get started quickly. We skip portfolio analysis and go straight into detailed assessment and planning.
> *Front-loaded EBA* - Phases 100 and 200 skipped, start at Phase 300 Path B. Recommend running ATX Comprehensive Codebase Analysis on each candidate app immediately.
>
> **B) Customer has a portfolio of applications and needs help deciding which to modernize first** - they need an inventory, feasibility scoring, and prioritized wave plan before diving into specific apps.
> *Full engagement (100 to 400)* - All phases active, start at Phase 100 with portfolio inventory.
>
> **C) Customer wants an assessment and recommendations but is not ready to commit to execution plans yet** - they want to understand the landscape, complexity, and options before planning.
> *Assessment only* - Phase 400 skipped, start at Phase 100.
>
> Which situation best describes {CUSTOMER}?

Wait for the user's answer before proceeding.

### Step 6 - Apply engagement type to progress table

Update the Progress table in `working/{CUSTOMER}/engagement.md` based on the user's answer:

- **Answer A (Front-loaded EBA):** Mark Phase 100 and Phase 200 as `Skipped`. Set engagement type field to "Front-loaded EBA".
- **Answer B (Full engagement):** Keep all phases as "Not started". Set engagement type field to "Full (100 to 400)".
- **Answer C (Assessment only):** Mark Phase 400 as `Skipped`. Set engagement type field to "Assessment only".

### Step 7 - Guide user on next steps

Based on the engagement type, explain which phase to start with and point the user to the relevant section in `working/{CUSTOMER}/engagement.md`:

- **Type A:** Start at Phase 300. Recommend gathering application source code and running ATX Comprehensive Codebase Analysis. Suggest invoking the Phase 300 skill next.
- **Type B:** Start at Phase 100. Ask the user to prepare or provide their application inventory spreadsheet. Suggest using the `excel-analysis` skill to work with the inventory.
- **Type C:** Start at Phase 100 (same as B, but note that Phase 400 will be skipped).

Mention any reference materials the user should prepare (e.g., application inventory for Phase 100, source code access for Phase 300).
