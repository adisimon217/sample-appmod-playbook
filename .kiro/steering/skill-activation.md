---
inclusion: always
---

# Skill Activation

This guide maps natural language intents to the appropriate skill. Users never need to mention skill names directly - infer the correct skill from context, intent, and workspace state.

## Intent Mapping

| User says something like... | Activate skill |
|-----------------------------|----------------|
| "I want to modernize my application" | `start-project` |
| "I want to modernize my .NET apps" | `start-project` |
| "I want to assess my portfolio for modernization" | `start-project` |
| "Let's start a new modernization project" | `start-project` |
| "I have apps I want to move to AWS" | `start-project` |
| "Help me plan my migration" | `start-project` |
| "I have an inventory ready" | `data-collection` |
| "Let's collect application data" | `data-collection` |
| "I need to populate the inventory spreadsheet" | `data-collection` |
| "I have reference documents to analyze" | `data-collection` |
| "Analyze the source code for inventory" | `data-collection` |
| "Analyze feasibility" | `feasibility-analysis` |
| "Score and prioritize my applications" | `feasibility-analysis` |
| "Which apps should we modernize first?" | `feasibility-analysis` |
| "Create a wave plan" | `feasibility-analysis` |
| "Run the detailed assessment" | `detailed-assessment` |
| "Let's do a deep dive on the selected apps" | `detailed-assessment` |
| "Start the questionnaires" | `detailed-assessment` |
| "Architecture workshop" | `detailed-assessment` |
| "Process meeting notes" | `detailed-assessment` |
| "Generate plans" | `generate-plans` |
| "Create the EBA plan" | `generate-plans` |
| "Produce the PoC plan" | `generate-plans` |
| "Generate modernization plans" | `generate-plans` |
| "I need to read an Excel file" | `excel-analysis` |
| "Analyze this spreadsheet" | `excel-analysis` |

## Workspace State Detection

Before activating a skill, check the current state of the `working/` folder to determine context:

1. **No project folders exist in `working/`** (other than sample data)
   - User likely needs `start-project`
   - Ask what they want to do if intent is unclear

2. **Project folder exists but is mostly empty** (`working/{PROJECT}/` with only the template structure)
   - Project was set up but no phase has started
   - Check engagement type in `engagement.md` to determine the right starting phase
   - If Full or Assessment: suggest `data-collection`
   - If Front-loaded: suggest `detailed-assessment`

3. **Phase 100 data exists** (`working/{PROJECT}/100-data-collection/{PROJECT}-App-Inventory.xlsx` is populated)
   - Phase 100 may be complete
   - Suggest `feasibility-analysis` if Phase 200 has not started

4. **Phase 200 data exists** (`working/{PROJECT}/200-mod-feasibility/{PROJECT}-Feasibility-Assessment.md`)
   - Phase 200 may be complete
   - Suggest `detailed-assessment` if Phase 300 has not started

5. **Phase 300 data exists** (`working/{PROJECT}/300-detailed-assessment/{PROJECT}-Detailed-Assessment.md`)
   - Phase 300 may be complete
   - Suggest `generate-plans` if Phase 400 has not started

Always check the Progress table in `engagement.md` for the definitive status of each phase.

## Disambiguation

When intent is ambiguous, ask a clarifying question rather than guessing. For example:

- "I want to work on my apps" -> Ask whether they want to start a new project, continue an existing one, or work on a specific phase
- "What's next?" -> Check workspace state and recommend the next logical phase based on progress

## Multiple Projects

If multiple project folders exist under `working/`, ask which project to work on before activating any skill. Use the project name from the folder structure.
