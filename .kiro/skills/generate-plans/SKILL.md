---
name: generate-plans
description: Generate Phase 400 execution plans for all applications in the project portfolio. Produces PoC plans (validate unknowns), EBA plans (6-week accelerated modernization), and full modernization plans (phased delivery for large/complex apps) in a single pass. Enforces correct sequencing - PoCs validate unknowns that EBAs depend on, EBAs establish patterns that full projects reuse. Includes post-generation readiness tracking via the EBA Preparation Checklist.
---

## Workflow

Follow these steps to generate all Phase 400 execution plans for the project. This skill produces all plan types in one pass, cross-referencing dependencies between them.

**Prerequisites:** Phase 300 must be complete. The detailed assessment should have been run and the consolidated assessment finalized with architecture decisions resolved.

**Plan sequencing principle:** PoC plans come first (they validate unknowns the EBA depends on). EBA plans come second (they establish patterns the full projects reuse). Full modernization plans come last (they reference patterns proven in earlier plans).

### Step 1 - Identify the project and validate Phase 300 readiness

Determine the project name (`{PROJECT}`) from context.

Verify that Phase 300 is complete by checking:
1. The consolidated assessment exists at `working/{PROJECT}/300-detailed-assessment/{PROJECT}-Detailed-Assessment.md`
2. Architecture working docs exist at `working/{PROJECT}/300-detailed-assessment/{PROJECT}-*-Architecture.md`
3. The engagement tracker `working/{PROJECT}/engagement.md` shows Phase 300 as complete

If Phase 300 is not complete, inform the user and recommend completing the detailed assessment first.

Read the consolidated assessment and all architecture working docs to understand:
- The execution path recommended for each application (PoC, EBA, or full project)
- The status of all technical decisions (AGREED, EXPLORING, DEFERRED)
- Cross-application dependencies and shared concerns

Also read the analysis rules from `.kiro/skills/detailed-assessment/references/detailed-analysis.md` for context on the decision criteria.

### Step 2 - Classify applications by execution path

From the consolidated assessment, build a plan generation matrix:

| Application | Execution Path | Reason | Dependencies |
|-------------|---------------|--------|--------------|
| {app/topic} | PoC | {EXPLORING decisions needing validation} | {none or other PoCs} |
| {app} | EBA | {< 100K LoC, ATX eligible, isolated} | {PoC prerequisites} |
| {app} | Full project | {> 100K LoC, heavy rewrite, complex} | {EBA patterns to reuse} |

**Classification criteria (from the Phase 300 assessment):**

| Type | When to assign | Duration | Template |
|------|---------------|----------|----------|
| PoC | Decisions marked EXPLORING that need validation before committing | 1-2 weeks | `references/PoC-Plan-Template.md` |
| EBA | < 100K LoC, ATX eligible, isolated, manageable complexity | 6 weeks | `references/EBA-Plan-Template.md` |
| Full project | > 100K LoC, heavy rewrite, ASMX with many consumers, 0% tests | Varies | Phased delivery (no fixed template) |

**Cross-cutting PoCs:** If multiple applications share the same EXPLORING decision (e.g., gMSA authentication), create a single PoC plan that covers all affected apps rather than duplicating the PoC per app.

Present the classification for confirmation:

> Based on the Phase 300 assessment, here is the plan generation matrix:
>
> **PoC plans ({count}):** {list topics}
> **EBA plans ({count}):** {list apps}
> **Full project plans ({count}):** {list apps}
>
> The sequencing will be: PoCs first (validate unknowns) -> EBAs (accelerated delivery using validated patterns) -> Full projects (phased delivery reusing EBA-proven patterns).
>
> Shall I proceed with generating all plans, or would you like to adjust any classifications?

Wait for confirmation before proceeding.

### Step 3 - Generate PoC plans

For each topic/decision that needs a PoC, generate a plan using the template at `references/PoC-Plan-Template.md`.

Use the sample at `references/sample-output/ADC-PoC-gMSA-Auth.md` as a reference for quality, detail level, and structure.

For each PoC plan:
1. Define the hypothesis clearly (one sentence stating what needs to be proven)
2. Create a conceptual architecture diagram (Mermaid) showing the specific concern being validated
3. List entry criteria (what must be ready before the PoC starts)
4. Create an execution plan with day-by-day activities (1-2 week timeline)
5. Define exit criteria with specific, measurable success conditions
6. Document the Go/No-Go decision: what happens if the PoC succeeds vs fails
7. List risks and mitigations

**Cross-referencing:** Each PoC plan must clearly state which EBA plans depend on its outcome. Use explicit references:
- "If PoC succeeds: proceed with {APP} EBA using validated pattern"
- "If PoC fails: re-scope {APP} EBA to use fallback approach"

Output each PoC plan as: `working/{PROJECT}/400-mod-plan/{PROJECT}-PoC-{TOPIC}.md`

### Step 4 - Generate EBA plans

For each application recommended for EBA, generate a full 6-week execution plan using the template at `references/EBA-Plan-Template.md`.

Use the sample at `references/sample-output/ADC-Supplier-Portal-EBA-Plan.md` as a reference for quality, detail level, and structure.

For each EBA plan:
1. Pull the application profile, modernization approach, and target architecture from the consolidated assessment and architecture working doc
2. Incorporate all AGREED decisions from the architecture working doc
3. Note any PoC prerequisites that must complete before the EBA starts (explicit dependency)
4. Define component-level modernization approach (port/rewrite/hybrid per component)
5. Create a target architecture diagram (Mermaid)
6. Define scope boundaries (in scope vs out of scope for the 6-week EBA)
7. Select tooling (ATX .NET, ATX SQL, Kiro, etc.)
8. Document technical highlights (auth, DB migration, integrations, etc.)
9. Create a 6-week timeline with week-by-week activities and a Day 1/Day 2 EBA Party schedule
10. Define development environment setup requirements
11. List risks with severity and mitigations
12. Document pending decisions and unvalidated assumptions

**Key principles for EBA plans (from the template):**
- Be decisive: recommend ONE approach per decision point, not a menu of options
- Use the project's actual name, not generic placeholders
- The plan should read as a clear execution blueprint, not a consulting options paper
- Target state: .NET on Amazon ECS Fargate (Linux) + Aurora PostgreSQL (or SQL Server if DB is parked)
- Target the latest .NET LTS version

**Cross-referencing:** Each EBA plan must:
- Reference any prerequisite PoC plans explicitly ("PoC-{TOPIC} must complete before this EBA starts")
- Note patterns it will establish that full project plans will reuse
- Reference the pilot EBA if this is a subsequent Wave application

Output each EBA plan as: `working/{PROJECT}/400-mod-plan/{PROJECT}-{APP}-EBA-Plan.md`

### Step 5 - Generate full modernization plans

For applications that exceed EBA scope (> 100K LoC, heavy rewrite, ASMX with many consumers, 0% tests), generate a phased delivery plan.

There is no fixed template for full project plans. Structure them as:

1. **Executive summary:** Why this app exceeds EBA scope and the phased delivery rationale
2. **Application profile:** Same detail level as EBA plans
3. **Phased delivery structure:**
   - Pre-work phase: environment setup, team onboarding, dependency resolution
   - Phase A-N: logical delivery increments (each 2-4 weeks, each producing a deployable increment)
   - Cross-team coordination: dependencies on other teams, shared services, integration points
4. **Pattern reuse:** Explicitly reference patterns established by the pilot EBA
5. **Target architecture:** Mermaid diagram showing the end-state
6. **Risk assessment:** Higher emphasis on scope management and incremental delivery risks
7. **Team and timeline:** Larger team composition, longer timeline with milestones

**Cross-referencing:** Each full project plan must:
- Reference the EBA that established the reusable patterns (auth, containerization, DB migration)
- Note which EBA-proven approaches apply directly vs which need adaptation
- Identify cross-application dependencies with other plans being generated

Output each full project plan as: `working/{PROJECT}/400-mod-plan/{PROJECT}-{APP}-Mod-Plan.md`

### Step 6 - Cross-reference and dependency validation

After generating all plans, review the complete set for consistency:

1. **Dependency chain validation:** Verify that every PoC prerequisite referenced in an EBA plan has a corresponding PoC plan generated. Verify that pattern references in full project plans point to actual EBA plans.

2. **Timeline sequencing:** Build a consolidated timeline showing:
   - PoCs run first (weeks 1-2)
   - EBAs start after their prerequisite PoCs complete (Go decision)
   - Full projects start after the pilot EBA establishes patterns
   - Parallel execution where no dependencies exist

3. **Cross-app consistency:** Verify that shared decisions (auth patterns, infrastructure, CI/CD) are consistent across all plans. Flag any contradictions.

4. **Dependency summary:** Create or update a summary at the top of each plan noting:
   - "Depends on: {list of prerequisite plans}"
   - "Enables: {list of plans that depend on this one}"

Present the dependency map:

> **Plan dependency map:**
>
> ```
> PoC-{TOPIC} (weeks 1-2)
>   └── {APP}-EBA-Plan (weeks 3-8, requires PoC Go decision)
>         └── {APP}-Mod-Plan (weeks 9+, reuses EBA patterns)
> ```
>
> All plans are consistent and cross-referenced. {Any issues found.}

### Step 7 - Post-generation: EBA Preparation Checklist

For each application with an EBA plan, walk through the EBA Preparation Checklist at `references/EBA-Preparation-Checklist.md`.

The checklist covers four phases of readiness:
1. **Application Data Gathering** - Questionnaire validation, source access, assessment tools run
2. **Environment and Access Setup** - AWS account, connectivity, tooling installed
3. **People and Logistics** - SMEs confirmed, war room booked, architecture diagrams ready
4. **EBA Day Readiness** - Assessments reviewed, target architecture agreed, success criteria defined

For each EBA plan generated:
1. Copy the checklist to `working/{PROJECT}/400-mod-plan/{PROJECT}-{APP}-EBA-Checklist.md`
2. Pre-populate items that are already satisfied based on Phase 300 work (questionnaire completed, architecture agreed, etc.)
3. Highlight items that need immediate action before the EBA can start

Present a readiness summary:

> **EBA Readiness Status:**
>
> | Application | Phase 1 | Phase 2 | Phase 3 | Phase 4 | Blockers |
> |-------------|---------|---------|---------|---------|----------|
> | {APP} | {x/y done} | {x/y done} | {x/y done} | {x/y done} | {blockers} |
>
> Items requiring immediate action: {list}

### Step 8 - Mark Phase 400 complete

1. Update the Progress table in `working/{PROJECT}/engagement.md` to mark Phase 400 as "Complete".
2. Create a plan index file at `working/{PROJECT}/400-mod-plan/README.md` listing all generated plans with their type, application, and dependencies.
3. Present a summary of all generated plans:

> Phase 400 Modernization Plan generation is complete. All plans are saved in:
> `working/{PROJECT}/400-mod-plan/`
>
> **Generated plans:**
> - PoC plans: {count} ({list topics})
> - EBA plans: {count} ({list apps})
> - Full project plans: {count} ({list apps})
>
> **Execution sequence:**
> 1. PoC plans (1-2 weeks each) - validate unknowns
> 2. EBA plans (6 weeks each) - accelerated modernization using validated patterns
> 3. Full project plans (phased) - larger efforts reusing EBA-proven patterns
>
> **Next steps:**
> - Review all plans for sign-off
> - Complete the EBA Preparation Checklists for each EBA application
> - Schedule PoCs first (they gate EBA start dates)
> - Begin execution in wave order: Pilot/PoC first, then Wave 1, Wave 2, etc.
