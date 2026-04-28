# Kickoff Meeting — Voyager
Date: March 30, 2026
Attendees: John (CTO), Priya (Head of Engineering), Marcus (DBA), Sarah (DevOps lead), AWS team (2 SAs)

## Background
Voyager is a mid-size payment processing company based in Singapore. ~400 employees. They process card payments for retail merchants across SEA (SG, MY, TH, ID, PH). Regulated by MAS (Monetary Authority of Singapore).

## Why modernize
- Windows Server + SQL Server licensing is killing them — John mentioned $800K/year across the portfolio
- They had an outage last month — the payment gateway went down for 2 hours during peak. Root cause was a single IIS server with no failover
- MAS is pushing them to improve DR capabilities — they need multi-AZ at minimum
- Their best .NET developers are leaving because nobody wants to work on .NET Framework 4.x anymore
- They want to move to containers but the team has zero experience

## Applications discussed
John walked us through their portfolio on a whiteboard. I took a photo (see initial-whiteboard.jpg).

Main apps:
1. **PayGate** — the core payment gateway. Processes card transactions in real-time. ASP.NET Web API on .NET Framework 4.6.2. This is their bread and butter. ~200K LoC. SQL Server 2019 Enterprise with Always On AG. They said it handles 50K transactions/hour at peak. Uses Windows Auth internally but API keys for merchant-facing endpoints.

2. **MerchantHub** — merchant self-service portal. ASP.NET MVC on .NET Framework 4.8. Merchants log in to view transactions, download reports, manage their accounts. ~60K LoC. Uses the same SQL Server as PayGate (shared DB!). Has Crystal Reports for merchant statements. About 3000 active merchants.

3. **BackOffice** — internal operations tool. ASP.NET WebForms with Telerik controls. .NET Framework 4.5. Used by ~80 ops staff for transaction monitoring, dispute management, merchant onboarding. ~90K LoC. Separate SQL Server 2016 Standard. TFS for source control.

4. **ReconcEngine** — batch reconciliation engine. Console app + 2 Windows Services. Runs nightly, matches transactions against bank settlement files. .NET Framework 4.8, C#. ~35K LoC. Reads from PayGate DB, writes to its own reconciliation DB.

5. **FraudWatch** — real-time fraud detection. This one is interesting — it's actually already on .NET 6 running on EC2 Linux. They modernized it last year themselves. Uses Redis for caching. ~25K LoC. John said "don't touch this one, it's fine."

6. **ComplianceReporter** — generates regulatory reports for MAS. Windows Service that runs weekly. .NET Framework 4.0. Uses SSRS for report generation. Tiny — maybe 8K LoC. But critical because MAS fines them if reports are late.

## Key concerns raised
- Priya: "PayGate cannot have any downtime during migration. We process payments 24/7. Even 5 minutes of downtime costs us $50K in lost transactions."
- Marcus: "PayGate and MerchantHub share the same SQL Server instance. You can't migrate one without the other."
- Sarah: "We have zero container experience. I've read about Docker but never used it in production."
- John: "I don't want to spend 2 years on this. Can we show something working in 3 months?"

## Infrastructure
- Everything on-prem in Equinix SG1 datacenter
- 2 SQL Server instances: one Enterprise (PayGate/MerchantHub), one Standard (BackOffice)
- ReconcEngine has its own small SQL Express DB
- ComplianceReporter connects to PayGate DB (read-only)
- VPN to AWS already exists (they use S3 for some archival)
- Active Directory for internal auth — DCs on-prem only (not on AWS yet)

## Next steps
- We'll send them the inventory spreadsheet
- John wants to see a feasibility assessment in 2 weeks
- Priya will assign someone to fill in the questionnaire for each app
- Marcus will run the SQL Server diagnostic scripts we recommended