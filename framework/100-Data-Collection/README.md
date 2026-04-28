# Phase 100 — Data Collection

Portfolio-level application and database discovery.

## Artefact

`100-App-Inventory.xlsx` — three sheets:

| Sheet | Content |
|-------|---------|
| Metadata | Customer name, date, assessor |
| Applications | One row per app: type, .NET version, hosting, dependencies, LOC, criticality, RTO/RPO (22 columns) |
| Databases | One row per DB: SQL Server version/edition, size, object counts, HA, features (19 columns) |

## How to Use

**Option A — Customer fills from scratch.** Send the blank spreadsheet. Walk through it in a 30-minute call, let them complete async.

**Option B — Pre-populate from existing artefacts.** If the customer has already shared architecture diagrams, meeting notes, docs, or source code, use Kiro to extract what we can and send a pre-filled spreadsheet for validation. Place artefacts in `working/{CUSTOMER}/reference/`.

### Pre-populate from documents

```
Read the reference materials in #Folder:working/{CUSTOMER}/reference/
and the template in #File:framework/100-Data-Collection/100-App-Inventory.xlsx.

Extract as much as possible to populate the Applications and Databases sheets.
Set Discovery Method to "Customer Interview" (meeting notes/slides) or
"Automated Discovery" (architecture diagrams/technical docs).
Leave blank where information is insufficient.

Save as working/{CUSTOMER}/100-data-collection/100-App-Inventory.xlsx.
Set Assessment Date = today.
```

### Pre-populate from source code

```
Analyze the source code in #Folder:working/{CUSTOMER}/reference/{REPO_NAME}
and the template in #File:framework/100-Data-Collection/100-App-Inventory.xlsx.

For each solution/project found, extract: App Type, Language, .NET Version, LoC,
Windows/IIS Dependencies, NuGet packages, Container Usage, connection strings,
data access method. Set Discovery Method to "Code Analysis".
Leave blank where information is insufficient.

Save as working/{CUSTOMER}/100-data-collection/100-App-Inventory.xlsx.
Set Assessment Date = today.
```

## After Pre-Population

1. Review for gaps and inconsistencies
2. Send to customer: "We pre-populated this from your materials. Please review, correct, and fill blanks."
3. When validated, proceed to [Phase 200](../200-Mod-Feasibility/README.md)
