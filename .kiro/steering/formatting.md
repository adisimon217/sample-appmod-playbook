---
inclusion: fileMatch
fileMatchPattern: "**/*.md"
---

# Document Formatting Standards

Consistent formatting across all framework outputs. Follow these conventions when generating or editing markdown documents.

## Mermaid Quadrant Charts

When using `quadrantChart` for prioritization matrices:
- Never place points on the boundary lines — avoid 0.50 on either axis
- Each point must sit clearly within one of the four quadrants
- Use coordinate ranges: Low = 0.10–0.45, High = 0.55–0.90
- Space points apart so labels don't overlap — minimum 0.10 distance between points on the same axis
- Keep labels short (app name only, no IDs or descriptions)

## Mermaid Architecture Diagrams

- Use `graph TD` (top-down) for architecture diagrams — vertical layout avoids horizontal compression
- Limit to ~10 nodes max — show core components only
- Use short, single-line labels — no `<br/>` line breaks in node text
- List supporting AWS services (ECR, Secrets Manager, SSM, CloudWatch) as a text note below the diagram, not as nodes
- Use `subgraph` only for the ECS cluster grouping, not for every service category
- External dependencies use dotted lines (`-.->`)
- Color palette (muted, professional):
  - Compute (ECS/EKS/Lambda): `fill:#2e6b8a,stroke:#1a3d4e,color:#fff`
  - Database (Aurora/RDS): `fill:#3b4cc0,stroke:#252f7a,color:#fff`
  - Cache (ElastiCache): `fill:#2d6a4f,stroke:#1b4332,color:#fff`
  - Auth (Cognito/AD): `fill:#7b2d3b,stroke:#4a1a24,color:#fff`
  - Dark/infrastructure: `fill:#495057,stroke:#343a40,color:#fff`
  - Success/done: `fill:#2d6a4f,stroke:#1b4332,color:#fff`
  - Iterative/active steps: `fill:#5a6c7d,stroke:#3d4f5f,color:#fff`

## Status Indicators

Use these consistently across architecture working docs, EBA plans, and checklists:
- 🟢 AGREED — decision finalized, no further discussion needed
- 🟡 EXPLORING — under discussion or PoC in progress
- 🔴 BLOCKED — needs resolution before EBA can proceed
- ⚪ DEFERRED — parked for post-EBA
- 📋 — value carried forward from a previous phase (pre-populated)
- ⚠️ — targeted follow-up question or caution flag
- 🔍 — item requiring further investigation or customer input

## Tables vs Lists

- Use tables for structured comparisons (component → tool → complexity, risk → severity → mitigation)
- Use tables for attribute/value pairs (application profile summary)
- Use lists for sequential steps, action items, or open questions
- Use numbered lists only when order matters (workflow steps, prioritized items)
- Use bullet lists for unordered items (discussion points, observations)

## PoC and EBA Plan Table Conventions

- **No Owner column.** Everything is owned by the customer. Remove Owner columns from Entry Criteria, Execution Plan, and Exit Criteria tables.
- **Entry Criteria:** columns are `#`, `Criterion`, `Status`
- **Execution Plan:** columns are `Sno`, `Activity`, `Status`
- **Exit Criteria:** columns are `#`, `Criterion`, `How to verify`, `Result`
- **Risks:** columns are `#`, `Risk`, `Mitigation`. Label each risk with `R01`, `R02`, etc.

## Headings

- `#` — document title only (one per file)
- `##` — major sections (Executive Summary, Technical Decisions, Meeting Log)
- `###` — subsections (individual decisions like TD-001, individual meetings by date)
- Don't go deeper than `###` — use bold text or tables instead

## General

- Be concise — these documents are working artefacts, not prose. Prefer short sentences.
- Use the customer's actual name, not "the customer"
- Document metadata (customer, application, date, author, duration) should use a compact table — not stacked bold lines
- Decision entries: Proposal → Customer position → Alternatives → Decision (keep each to 1–3 sentences)
- Meeting logs: Discussed → Decided → Actions → Open (bullet points, not paragraphs)
- Code blocks for Kiro prompts — use triple backticks with no language tag
- When referencing files in prompts, use `#File:path/to/file.md` syntax