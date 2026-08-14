# {CUSTOMER} — Modernization Engagement

| Field | Value |
|---|---|
| Customer | {CUSTOMER} |
| Contact | {contact name / team} |
| Started | {date} |
| Engagement type | {Full (100→400) / Front-loaded EBA / Assessment only} |
| Applications | {count} apps in scope |

---

## Progress

| Phase | Status | Output |
|-------|--------|--------|
| 100 — Data Collection | ⬜ Not started | `100-data-collection/{CUSTOMER}-App-Inventory.xlsx` |
| 200 — Mod Feasibility | ⬜ Not started | `200-mod-feasibility/{CUSTOMER}-Feasibility.md` |
| 300 — Detailed Assessment | ⬜ Not started | `300-detailed-assessment/{CUSTOMER}-Detailed-Assessment.md` |
| 400 — Mod Plan | ⬜ Not started | `400-mod-plan/` |

Update status as you go: ⬜ Not started → 🔄 In progress → ✅ Done → ⏭️ Skipped

---

## How to Use

**Invoke Kiro skills** to progress through each phase. Skills are the primary way to interact with this framework - they guide you step by step, reference the correct templates, and produce outputs in the right locations.

| Phase | Skill to invoke | What it does |
|-------|----------------|--------------|
| Setup | `start-engagement` | Creates folder structure and this file |
| 100 | `data-collection` | Guides portfolio inventory collection |
| 200 | `feasibility-analysis` | Classifies, scores, and prioritizes applications |
| 300 | `detailed-assessment` | Per-app deep dive, questionnaires, and architecture workshops |
| 400 | `generate-plans` | Produces PoC, EBA, and full project plans |

Invoke skills by name (e.g., "run the data-collection skill" or "invoke feasibility-analysis"). Each skill picks up where the previous one left off.

## Phase Instructions (Reference)

The framework README files provide additional context on each phase:
- [100-data-collection/README.md](100-data-collection/README.md)
- [200-mod-feasibility/README.md](200-mod-feasibility/README.md)
- [300-detailed-assessment/README.md](300-detailed-assessment/README.md)
- [400-mod-plan/README.md](400-mod-plan/README.md)

These contain the underlying prompts and rules. The skills above wrap these into guided workflows.
