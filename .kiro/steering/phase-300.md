---
inclusion: fileMatch
fileMatchPattern: "{framework/300-Detailed-Assessment/**,working/*/300-detailed-assessment/**}"
---

# Phase 300 — Detailed Assessment Rules

## Questionnaire pre-population — application selection

When pre-populating questionnaires in Phase 300, follow this selection process:

## Step 1 — Present the portfolio

Read the feasibility assessment to get the wave plan and prioritization matrix.

- If the portfolio has fewer than 20 applications, list them all in a table: App ID, App Name, Wave, Execution Path, Complexity.
- If 20 or more, show only the summary counts per wave (Pilot: N apps, Wave 1: N apps, etc.) and list the Pilot/PoC and Wave 1 apps by name.

## Step 2 — Suggest 1–2 applications

Pick from the prioritization matrix in this order:
1. Pilot/PoC candidates first (Quick Win quadrant, isolated, low complexity)
2. If no Pilot/PoC, pick Wave 1 Quick Wins
3. If no Quick Wins, pick the lowest-complexity app from Wave 1

Suggest at most 2 applications. Explain briefly why each was selected (e.g., "lowest complexity, isolated, GA eligible").

## Step 3 — Confirm with user

Ask the user to confirm the selection before generating any questionnaires. Example:

> Based on the wave plan, I suggest starting with:
> - APP-004 ReconcEngine (Pilot — 35K LoC, isolated, 60% test coverage)
> - APP-002 MerchantHub (Wave 1 — 60K LoC, MVC, straightforward port)
>
> Shall I pre-populate questionnaires for these two? Or would you like to pick different apps?

Wait for confirmation. Do not generate questionnaires until the user confirms.

## Additional applications

The user can request questionnaires for additional applications at any time using the "pre-populate for a specific app" prompt. This does not require re-running the selection process.