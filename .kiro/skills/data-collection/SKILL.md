---
name: data-collection
description: Execute Phase 100 portfolio-level application and database discovery. Guides the user through populating the App Inventory spreadsheet - either from scratch (customer fills) or pre-populated from existing reference materials (documents, meeting notes, architecture diagrams, or source code). Produces a validated inventory ready for feasibility analysis.
---

## Workflow

Follow these steps in order to complete Phase 100 data collection for a customer engagement.

### Step 1 - Identify the customer and locate the working folder

Determine the customer name (`{CUSTOMER}`) from context. Confirm the working folder exists at `working/{CUSTOMER}/100-data-collection/`. If the engagement has not been set up yet, ask the user to invoke the `start-engagement` skill first.

### Step 2 - Determine the data collection approach

Ask the user which approach to use:

> How would you like to populate the application inventory?
>
> **Option A - Customer fills from scratch**
> Send the blank spreadsheet template to the customer. Walk through it in a 30-minute call, let them complete async.
>
> **Option B - Pre-populate from existing documents**
> If the customer has already shared architecture diagrams, meeting notes, docs, or other reference materials, I can extract what we can and produce a pre-filled spreadsheet for customer validation.
>
> **Option C - Pre-populate from source code**
> If the customer has provided application source code repositories, I can analyze the code to extract application details (type, language, .NET version, LOC, dependencies, connection strings, etc.).
>
> Which approach works best for this engagement?

Wait for the user's answer before proceeding.

### Step 3 - Execute the chosen approach

#### Option A - Customer fills from scratch

1. Confirm the blank template exists at `working/{CUSTOMER}/100-data-collection/{CUSTOMER}-App-Inventory.xlsx` (copied during engagement setup from `references/100-App-Inventory.xlsx`).
2. If it does not exist, copy `references/100-App-Inventory.xlsx` to `working/{CUSTOMER}/100-data-collection/{CUSTOMER}-App-Inventory.xlsx`.
3. Advise the user to send the spreadsheet to the customer with these instructions:
   - Fill one row per application in the Applications sheet (22 columns covering type, .NET version, hosting, dependencies, LOC, criticality, RTO/RPO)
   - Fill one row per database in the Databases sheet (19 columns covering SQL Server version/edition, size, object counts, HA, features)
   - Fill the Metadata sheet with customer name, date, and assessor
4. Recommend a 30-minute walkthrough call to explain columns, then let the customer complete async.
5. Once returned, proceed to Step 4.

#### Option B - Pre-populate from documents

1. Confirm reference materials are placed in `working/{CUSTOMER}/reference/`. If not, ask the user to add architecture diagrams, meeting notes, docs, or slides to that folder.
2. Use the `excel-analysis` skill to work with the inventory spreadsheet.
3. Read all reference materials in `working/{CUSTOMER}/reference/`.
4. Read the template structure from `references/100-App-Inventory.xlsx`.
5. Extract as much information as possible to populate the Applications and Databases sheets:
   - Set Discovery Method to "Customer Interview" for information from meeting notes or slides
   - Set Discovery Method to "Automated Discovery" for information from architecture diagrams or technical documentation
   - Leave cells blank where information is insufficient - do not guess
6. Save the pre-populated inventory as `working/{CUSTOMER}/100-data-collection/{CUSTOMER}-App-Inventory.xlsx`.
7. Set Assessment Date to today's date in the Metadata sheet.
8. Proceed to Step 4.

#### Option C - Pre-populate from source code

1. Ask the user for the repository path(s). These should be placed in `working/{CUSTOMER}/reference/{REPO_NAME}` or the user can provide absolute paths.
2. Use the `excel-analysis` skill to work with the inventory spreadsheet.
3. Read the template structure from `references/100-App-Inventory.xlsx`.
4. For each solution/project found in the source code, extract:
   - Application Type (from project type - .csproj, web.config, etc.)
   - Programming Language (C#, VB.NET, F#, etc.)
   - Major .NET Version (from TargetFramework or TargetFrameworkVersion)
   - Lines of Code (count source files)
   - Windows Dependencies (from app.config, registry usage, service installers)
   - IIS Dependencies (from web.config, applicationHost.config)
   - NuGet packages (from packages.config or PackageReference)
   - Container Usage (from Dockerfile, docker-compose, Kubernetes manifests)
   - Connection strings (from config files, appsettings.json)
   - Data access method (Entity Framework, Dapper, ADO.NET, etc.)
5. Set Discovery Method to "Code Analysis" for all extracted rows.
6. Leave cells blank where information is insufficient - do not guess.
7. Save the pre-populated inventory as `working/{CUSTOMER}/100-data-collection/{CUSTOMER}-App-Inventory.xlsx`.
8. Set Assessment Date to today's date in the Metadata sheet.
9. Proceed to Step 4.

### Step 4 - Review for gaps and inconsistencies

After population (any approach), review the inventory for:

- Missing critical fields (App Type, .NET Version, Business Criticality)
- Inconsistencies (e.g., .NET Framework version listed but Container Usage = Kubernetes)
- Applications referenced in the Databases sheet that are not in the Applications sheet
- Databases without a linked App ID
- Duplicate entries

Present a summary of gaps to the user and recommend which items to clarify with the customer.

### Step 5 - Send for customer validation (Option B and C only)

If the inventory was pre-populated, advise the user to send it to the customer with this message:

> "We pre-populated this inventory from your materials. Please review, correct any inaccuracies, and fill in the blank cells where you have the information."

Wait for the validated inventory to be returned and updated at `working/{CUSTOMER}/100-data-collection/{CUSTOMER}-App-Inventory.xlsx`.

### Step 6 - Mark Phase 100 complete and proceed

Once the inventory is validated:

1. Update the Progress table in `working/{CUSTOMER}/engagement.md` to mark Phase 100 as "Complete".
2. Inform the user that data collection is finished.
3. Recommend invoking the `feasibility-analysis` skill to proceed to Phase 200.

> Phase 100 Data Collection is complete. The validated inventory is ready at:
> `working/{CUSTOMER}/100-data-collection/{CUSTOMER}-App-Inventory.xlsx`
>
> To proceed with modernization feasibility scoring and wave planning, invoke the `feasibility-analysis` skill.
