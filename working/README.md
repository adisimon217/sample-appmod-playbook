# Working — Customer Engagements

Each customer gets a subfolder. Start a new engagement using the prompt in the [root README](../README.md).

```
working/{CUSTOMER}/
  engagement.md                           # Runbook — prompts, status, links
  reference/                              # Customer-provided artefacts (read-only input)
  100-data-collection/
    {CUSTOMER}-App-Inventory.xlsx
  200-mod-feasibility/
    {CUSTOMER}-Feasibility.md
  300-detailed-assessment/
    {CUSTOMER}-{APP}-Questionnaire.md     # One per app
    {CUSTOMER}-{APP}-Architecture.md      # One per app with discussion points
    {CUSTOMER}-Detailed-Assessment.md     # Consolidated output
    meetings/                             # Raw meeting notes, photos, transcripts
  400-mod-plan/
    {CUSTOMER}-PoC-{TOPIC}.md             # PoC plans
    {CUSTOMER}-{APP}-EBA-Plan.md          # EBA plans
    {CUSTOMER}-{APP}-Mod-Plan.md          # Full project plans
```

DISCLAIMER:

The sample folder contains artefacts that pertains to a fictitious customer to serve as an example of how to use this framework.