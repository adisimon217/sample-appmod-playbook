---
name: detailed-assessment
description: Execute Phase 300 per-application deep dive for workloads selected in Phase 200. Guides through application selection, questionnaire pre-population (with optional ATX codebase analysis), consolidated assessment generation, and iterative architecture refinement workshops until all decisions are agreed or deferred. Produces a consolidated assessment document that feeds into Phase 400 plan generation.
---

## Workflow

Follow these steps to complete Phase 300 detailed assessment for a customer engagement. Steps 1-4 run once. Steps 5-7 repeat until all architecture decisions are resolved.

### Step 1 - Identify the customer and determine the assessment path

Determine the customer name (`{CUSTOMER}`) from context. Verify the working folder `working/{CUSTOMER}/300-detailed-assessment/` exists.

Determine which path applies:

- **Path A (Standard - from Phase 200):** The feasibility assessment exists at `working/{CUSTOMER}/200-mod-feasibility/{CUSTOMER}-Feasibility-Assessment.md` and the inventory exists at `working/{CUSTOMER}/100-data-collection/{CUSTOMER}-App-Inventory.xlsx`. Proceed to Step 2.

- **Path B (EBA front-loaded - no prior phases):** No Phase 100/200 data exists. The customer has identified specific applications for modernization without going through portfolio discovery. Skip Step 2 and go directly to Step 3 (Path B variant).

If Path A prerequisites are missing but the user expects them to exist, ask them to invoke the `feasibility-analysis` skill first.

### Step 2 - Select applications for detailed assessment

Read the feasibility assessment from `working/{CUSTOMER}/200-mod-feasibility/{CUSTOMER}-Feasibility-Assessment.md` using the wave plan and prioritization matrix.

**Portfolio display rules:**

- **If fewer than 20 applications** in the portfolio: Present all applications in a table showing App Name, Quadrant (Quick Wins / Strategic / Selective / Low-Priority), Wave Assignment, Combined Complexity Score, and ATX Eligibility.

- **If 20 or more applications** in the portfolio: Show summary counts per quadrant and wave, then list only Pilot/PoC and Wave 1 applications by name in a table. Note that additional applications can be assessed at any time by re-invoking this skill.

**Application suggestion:**

Suggest 1-2 applications to assess first, selected from the Quick Wins quadrant using these priorities:
1. Pilot/PoC application (mandatory first selection)
2. Wave 1 Quick Wins (if a second application is appropriate)

Present the suggestion with rationale:

> Based on the feasibility assessment, I recommend starting detailed assessment with:
>
> 1. **{APP_NAME}** (Pilot/PoC) - {rationale: e.g., lowest complexity in Quick Wins, GA eligible for ATX .NET, C# source in Git}
> 2. **{APP_NAME}** (Wave 1 Quick Win) - {rationale}
>
> These applications will proceed through questionnaire completion and architecture workshops.
> Additional applications can be added at any time by re-running this skill.
>
> Shall I proceed with these applications, or would you like to adjust the selection?

Wait for user confirmation before proceeding to Step 3. The user may request different applications or additional ones.

### Step 3 - Pre-populate questionnaires

For each selected application, create a questionnaire from the template at `framework/300-Detailed-Assessment/300-App-Questionnaire.md`.

#### Path A - Pre-populate from Phase 100/200 data

Use the `excel-analysis` skill to read inventory data from `working/{CUSTOMER}/100-data-collection/{CUSTOMER}-App-Inventory.xlsx`, then combine with the feasibility assessment to pre-populate answers.

**Field mapping (Phase 100/200 to Questionnaire):**

| Questionnaire Section | Source | Fields |
|-----------------------|--------|--------|
| 1. App Overview | Inventory (Apps) | App Name, Description, Criticality, Users, RTO/RPO |
| 2. Architecture | Inventory (Apps) | App Type, Language, .NET Version, LoC, Windows/IIS Deps, Containers |
| 2. Architecture | Feasibility | Classification, ATX eligibility, blockers, pathway |
| 4. Dependencies | Inventory (Apps) | Deployment Method |
| 7. Hosting | Inventory (Apps) | Current Hosting, RTO/RPO |
| 9. DevOps | Inventory (Apps) | Source Repo, Deployment, Change Frequency |
| 12. Database | Inventory (DBs) | Version/Edition, Object Counts, Size, HA, Workload, Features, Auth, Access Method |
| 12. Database | Feasibility | ATX SQL eligibility, blockers |
| Data gaps | Feasibility | Targeted follow-up questions per app |

**If ATX Comprehensive Codebase Analysis output is available** (recommended):

Check if output exists at `working/{CUSTOMER}/200-mod-feasibility/{APP_NAME}-codebase-analysis/`. If available, use it as the primary source - it provides high-confidence answers for architecture, dependencies, code quality, DB access patterns, and integration points. This significantly reduces the number of gaps requiring customer follow-up.

When pre-populating from codebase analysis:
- Mark answers derived from codebase analysis with a clipboard icon (carried forward from analysis)
- Mark remaining gaps with a warning icon for customer follow-up
- Expect significantly fewer gaps compared to inventory-only pre-population

If codebase analysis has not been run but source code is accessible, recommend running it:

> **Recommended: ATX Comprehensive Codebase Analysis**
>
> Source code is available for {APP_NAME}. Running the comprehensive codebase analysis before questionnaire completion will provide measured metrics (actual LOC, dependency maps, specific blockers) that reduce customer interview time.
>
> To run: copy `framework/200-Mod-Feasibility/assets/atxconfig-coda.yaml` to `working/{CUSTOMER}/200-mod-feasibility/atxconfig-coda.yaml` and execute from that directory:
> ```
> atx custom def exec -t -n AWS/comprehensive-codebase-analysis -g file://atxconfig-coda.yaml -p <repo-path>
> ```
>
> Would you like to run codebase analysis first, or proceed with inventory-based pre-population?

#### Path B - EBA front-loaded (no prior phases)

Copy the blank questionnaire template directly:
1. Copy `framework/300-Detailed-Assessment/300-App-Questionnaire.md` to `working/{CUSTOMER}/300-detailed-assessment/{CUSTOMER}-{APP}-Questionnaire.md` for each application.
2. If source code is accessible, recommend running ATX Comprehensive Codebase Analysis (same process as above) and use output to pre-populate where possible.
3. Mark all fields as requiring customer input (no inventory data available to carry forward).

#### Customer walkthrough

For both paths, walk the customer through each questionnaire:
- Focus on items marked with a warning icon (gaps) and any blank fields
- Let the customer complete asynchronously over 1-2 weeks
- Track completion status and follow up on outstanding items

Output each questionnaire as: `working/{CUSTOMER}/300-detailed-assessment/{CUSTOMER}-{APP}-Questionnaire.md`

### Step 4 - Generate the consolidated assessment and architecture working docs

Once questionnaires are returned (or sufficiently complete), generate the consolidated assessment.

**Consolidated assessment generation:**

Read the analysis rules from `framework/300-Detailed-Assessment/detailed-analysis.md`. For each application:

1. Interpret every questionnaire answer using the decision matrices in `detailed-analysis.md`
2. Determine modernization approach, tooling, and target architecture
3. Flag high-risk, high-complexity, or multi-option decisions as Discussion Points
4. Recommend execution path: PoC (if high-risk unknowns), EBA (if suitable), or full modernization project (if too large/complex for EBA)
5. Flag unanswered questions as assumptions with impact-if-incorrect

Produce a single consolidated document containing:
- Portfolio summary (updated from Phase 200 with new detail)
- Per-app sections: profile, approach, target architecture, risks, execution path
- Cross-app concerns (shared dependencies, auth patterns, DB migration sequencing)
- Consolidated discussion points across all apps

Output as: `working/{CUSTOMER}/300-detailed-assessment/{CUSTOMER}-Detailed-Assessment.md`

**Architecture working docs:**

For each application that has discussion points, create an architecture working doc from the template at `framework/300-Detailed-Assessment/300-Architecture.md`:
- Populate the Draft Architecture diagram based on the target architecture
- Create TD-### entries (status: EXPLORING) for each discussion point
- Include initial observations from the assessment

Output as: `working/{CUSTOMER}/300-detailed-assessment/{CUSTOMER}-{APP}-Architecture.md`

### Step 5 - Architecture workshops (iterative)

Walk through EXPLORING decisions with the customer. Workshops can be organized per-app or grouped by theme (e.g., an auth workshop covering all apps that use Windows Auth).

Guide the user to:
- Schedule focused sessions (30-60 minutes per topic or app)
- Prepare by reviewing the relevant TD-### entries beforehand
- Capture raw notes during the session (typed notes, photos, chat logs)

Save meeting notes as: `working/{CUSTOMER}/300-detailed-assessment/meetings/{CUSTOMER}-{APP}-Meeting-{DATE}.md`

After the workshop, proceed to Step 6 to process the notes.

### Step 6 - Process meeting notes into architecture working docs (iterative)

Read the meeting notes from `working/{CUSTOMER}/300-detailed-assessment/meetings/{CUSTOMER}-{APP}-Meeting-{DATE}.md` and update the corresponding architecture working doc:

1. Add a Meeting Log entry: date, attendees, topics discussed, decisions made (TD-###), action items, open items
2. Update each Technical Decision discussed:
   - Record customer position, new alternatives explored, status change (AGREED / BLOCKED / EXPLORING)
3. Add new observations and any new TD-### entries that surfaced during the workshop
4. Update the Draft Architecture diagram if agreed decisions changed it

Save the architecture working doc in place.

After processing, proceed to Step 7 to regenerate the consolidated assessment.

### Step 7 - Regenerate the consolidated assessment (iterative)

Re-read all questionnaires (`working/{CUSTOMER}/300-detailed-assessment/*-Questionnaire.md`), all architecture working docs (`working/{CUSTOMER}/300-detailed-assessment/*-Architecture.md`), the feasibility assessment, and the analysis rules from `framework/300-Detailed-Assessment/detailed-analysis.md`.

Update the consolidated assessment:

1. Incorporate all AGREED decisions from architecture working docs
2. For EXPLORING decisions, note both options as pending
3. For BLOCKED decisions, flag prominently in risks section
4. Update per-app architecture diagrams and execution path recommendations
5. Update cross-app concerns, discussion points, and assumptions

Overwrite: `working/{CUSTOMER}/300-detailed-assessment/{CUSTOMER}-Detailed-Assessment.md`

**Iteration check:**

- If BLOCKED items remain: they must be resolved before proceeding. Return to Step 5 with focused workshops on the blocked topics.
- If only EXPLORING items remain: they are acceptable if they are tracked as PoC validation items or have clear plans to be resolved prior to execution. The user may choose to continue iterating or proceed.
- If all items are AGREED or DEFERRED: Phase 300 is complete. Proceed to Step 8.

Present the current decision status summary:

> **Architecture Decision Status:**
> - AGREED: {count}
> - EXPLORING: {count}
> - BLOCKED: {count}
> - DEFERRED: {count}
>
> {Recommendation: continue iterating or proceed to Phase 400}

### Step 8 - Mark Phase 300 complete and proceed

1. Update the Progress table in `working/{CUSTOMER}/engagement.md` to mark Phase 300 as "Complete".
2. Present a summary of the key outcomes:
   - Applications assessed (count and names)
   - Execution paths assigned (how many PoC, EBA, full project)
   - Architecture decisions resolved vs deferred
   - Key risks and open items carried forward
3. Recommend next steps:

> Phase 300 Detailed Assessment is complete. The consolidated assessment is saved at:
> `working/{CUSTOMER}/300-detailed-assessment/{CUSTOMER}-Detailed-Assessment.md`
>
> **Next steps:**
> - Review the consolidated assessment with the customer for final sign-off
> - Ensure all DEFERRED items have owners and target dates
> - Invoke the `generate-plans` skill to produce execution plans (PoC plans, EBA plans, or full modernization project plans) for each application based on its assigned execution path
>
> Applications should proceed to plan generation in wave order: Pilot/PoC first, then Wave 1, Wave 2, etc.
