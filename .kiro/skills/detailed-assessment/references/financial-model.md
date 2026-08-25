# Financial Model — TCO, ROI & Cost of Inaction

**Purpose:** Produce the financial business case and the numbers that drive the executive deliverables (CEO Brief, Exec Outbrief). No AWS Transform job computes financials — the primary inputs are cost and effort data that cannot be derived from source code. This is a modelling layer with stated assumptions.

**Consumed by:** Portfolio-level business case synthesis — draws on Phase 100 (cost data), Phase 200 (wave plan, pathways), and Phase 300 (effort, security/EOL findings). If a dedicated business-case/executive-readout phase (candidate Phase 350) is introduced, this reference anchors it.

**Status of output:** Assessment-grade planning estimate. Every figure carries an explicit assumption and a confidence band (default ±15% at 80% CI). Direct the customer to AWS Pricing Calculator / their account team for contractual pricing. A human assumptions review is **mandatory** before this reaches a CFO.

---

## Section 1 — Model Inputs

| Input | Source | Notes |
|-------|--------|-------|
| Current-state cost (compute, DB licensing, labor, risk) | Phase 100 inventory cost columns | Must be captured; ATX cannot derive it |
| Target-state cost (Linux/Graviton compute, Aurora) | AWS Price List API (`pricing:GetProducts`) or AWS Pricing Calculator | List-price; caveat with discount note |
| Migration effort (person-days) | Effort model (Section 4), seeded from MODA classification + findings volume | Converts to labor cost |
| Blended day rate | Customer or delivery-partner rate | Default placeholder; must be confirmed |
| EOL / ESU pricing | Vendor extended-support schedules | For cost-of-inaction |
| AWS funding eligibility | MAP / MMP program terms | Offsets net investment |

Capture the confirmed rate and discount assumptions in a parameters block at the top of the generated business case so every downstream number is traceable.

---

## Section 2 — Current-State TCO

Sum the annual cost across these categories:

| Category | How to compute |
|----------|----------------|
| Compute | Instance count × instance type rate × environment multiplier (prod + non-prod) + Windows licensing premium |
| Database | SQL Server instance count × edition licensing (Enterprise/Standard) + SA/maintenance |
| Operational labor | Maintenance FTE count × loaded FTE cost × % time on legacy |
| Technical debt (unplanned work) | % of dev time on firefighting × dev cost (default 20–30%) |
| Security risk exposure | Estimated breach cost × annualized probability (from open-CVE count) |

Output: annual TCO with per-category breakdown and % of total.

---

## Section 3 — Target-State Cost (3-Year Projection)

1. Map each app to its recommended target architecture (from MODA pathways + Appendix B target-architecture table).
2. Price each component via the AWS Price List API. Use the `use_aws` tool, `pricing` service, `GetProducts` operation, filtering on service code, region, instance attributes. Example services: `AmazonEC2` (or Fargate via `AmazonECS`/`AmazonEKS`), `AmazonRDS`/Aurora, `AmazonElastiCache`.
3. Model three years: Year 1 (investment + partial run-rate), Year 2 (savings begin), Year 3 (full run-rate). Include new AI/agent infrastructure cost from Year 2 if in scope.
4. Add the one-time modernization investment (Section 4) to Year 1.

Output: per-year total, savings vs. current (absolute and %).

**Pricing caveat:** Price List API returns list pricing. Note that actual cost is typically 10–20% lower with Reserved Instances / Savings Plans / EDP. State estimates conservatively.

---

## Section 4 — Effort → Investment

Effort drives the one-time investment. Seed person-days from the MODA readiness classification and pathway, then break down by category:

| Effort category | Driver |
|-----------------|--------|
| Framework upgrade | .NET version gap × LOC band |
| UI rewrite | Web Forms / WinForms / WPF page count (highest-variance line) |
| DB migration | SP count + T-SQL incompatibility volume |
| API exposure | Endpoints to expose for target/AI readiness |
| Testing | % of build effort (default 20%) |
| Deployment | Containerization + pipeline setup |

Investment = total person-days × blended day rate. Add contingency per confidence level (Section 7).

**Pathway → confidence seed (from MODA classification):**

| MODA classification | Typical pathway | Confidence seed |
|---------------------|-----------------|-----------------|
| Cloud-Native Ready | M-Upgrade | 90%+ |
| Pilot-Ready | M-Basic / M-Upgrade | 75–89% |
| Remediation Required | M-Basic / M-Decompose | 60–74% |
| Not Modernization Ready | M-Decompose / Retire | <60% |

---

## Section 5 — Savings

Savings = Current-state TCO − Target-state annual cost. Attribute to sources so the business case is defensible:

| Savings source | Mechanism |
|----------------|-----------|
| Compute licensing | Windows → Linux/Graviton (eliminate Windows premium) |
| Database licensing | SQL Server Enterprise → Aurora (eliminate edition licensing) |
| Operational labor | Reduced legacy maintenance on modern managed stack |
| Security risk | CVEs resolved → reduced breach exposure |

---

## Section 6 — Cost of Inaction

The compounding annual cost of deferring modernization. Each line requires a stated, caveated assumption:

| Risk | Assumption to state |
|------|---------------------|
| Database EOL — extended support (ESU) + breach exposure | Vendor ESU pricing schedule; escalates annually |
| Framework EOL — unpatched security gap | Compounding unpatched-vulnerability exposure |
| Legacy talent premium | Market rate premium growth (default ~15% YoY) |
| Security breach probability | Increases per unpatched month; probability × impact |
| Competitive / AI gap | Revenue at risk vs. peers deploying AI (widest band — flag as directional) |

Output: per-year (Y1/Y2/Y3) and cumulative 3-year cost of inaction.

---

## Section 7 — ROI, Payback & Scenarios

Standard formulas over 3 years:

- **Payback period** = months until cumulative savings ≥ investment.
- **3-year ROI** = (3-yr cumulative savings − investment) / investment.
- **NPV** = discounted net cash flows (default 8% discount rate — state it).
- **IRR** = discount rate at which NPV = 0.
- **Break-even month** = from the cumulative cash-flow timeline (include ramp-up).

**Scenario analysis** (probability-weighted expected value):

| Scenario | Probability (default) | Assumptions |
|----------|----------------------|-------------|
| Best case | 20% | On time + full AI adoption + Graviton + all funding |
| Base case | 60% | On schedule, partial AI adoption, base funding |
| Worst case | 20% | Delay, waves deferred, no AI, no funding |

Expected value = Σ (scenario net savings × probability).

---

## Section 8 — AWS Funding

Reduce net investment via program offsets:

| Program | Typical role | Requirement |
|---------|-------------|-------------|
| MAP (Migration Acceleration Program) | Credits across assess/mobilize/migrate | Documented phases |
| MMP (Migration & Modernization Program) | Committed-workload funding | Partner engagement |

Output: gross investment, funding offset range, net investment after funding, effective payback after funding. Note programs are point-in-time — apply early.

---

## Section 9 — Confidence & Caveats

| Confidence | Meaning | When |
|-----------|---------|------|
| 90–100% | Strong data, clear path | Simple upgrades, confirmed costs |
| 75–89% | Good, manageable unknowns | Standard modernization |
| 60–74% | Mixed signals, some risk | Complex apps, multiple pathways |
| <60% | Significant unknowns | Large monoliths, unconfirmed cost inputs |

**Mandatory caveats on every financial output:**
- Estimates are assessment-grade planning figures at ±15% (80% CI), based on list pricing and stated assumptions.
- Actual AWS cost varies with RIs / Savings Plans / EDP (typically 10–20% lower).
- Contractual pricing must come from AWS Pricing Calculator / the customer's AWS account team.
- Cost-of-inaction figures — especially the competitive/AI gap — are directional and depend on customer-specific inputs.
- A human review of the parameters block is required before presenting to finance stakeholders.
