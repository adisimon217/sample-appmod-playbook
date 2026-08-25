# Compliance Crosswalk — ATX Findings → SOC 2 / PCI-DSS / ISO 27001

**Purpose:** Produce the compliance gap analysis without a dedicated compliance scan. No AWS Transform managed job emits a SOC 2 / PCI-DSS / ISO 27001 map, but the *evidence* those controls require is already produced by the analysis chain. This document is the crosswalk that maps chain findings to control frameworks and defines how to emit the pre/post gap table.

**Consumed by:** Phase 300 (`detailed-assessment`) — after the security, MODA, and ARA jobs have run for the assessed applications.

**Status of output:** Assessment-grade. This crosswalk is an evidence-backed *indicator* of compliance posture, not an audit. Every output must carry the caveat that formal attestation requires an auditor and (for post-modernization state) runtime evidence.

---

## Section 1 — Inputs

The crosswalk reads the machine-readable `*-report.json` artifacts (authoritative if artifacts disagree) from three jobs. Never parse the `.html`; parse `.json` and cite the `.md` for narrative evidence.

| Source job | Artifact | Signals used |
|------------|----------|--------------|
| Continuous Modernization `--type security` | `*-security findings` | CVEs (CVSS, component, fix version), insecure coding patterns, hardcoded secrets, weak crypto |
| `AWS/modernization-readiness-analysis` (MODA) | `*-moda-report.json` | Security Baseline category (secrets management, network segmentation, encryption, compliance automation); Operations & Observability (CI/CD maturity, logging, deployment) |
| `AWS/agentic-readiness-analysis` (ARA) | `*-ara-report.json` | Security & identity pillar (auth boundaries, MFA, machine identity); Observability pillar (structured logging, tracing) |

If any of the three has not been run for an app, mark the affected controls **🔍 Not Assessed** rather than assuming pass or fail.

---

## Section 2 — Process

1. For each assessed app, load the three JSON artifacts.
2. For each **finding signal** in Section 3, check whether the chain reported it. A reported finding = a **GAP** against the mapped control(s).
3. Aggregate per control: a control is a portfolio GAP if *any* in-scope app exhibits the signal.
4. Determine the **post-modernization status** using the "Closed by" column — the target-state AWS control that resolves it (Section 4).
5. Emit the Ch8 compliance gap table (Section 5).
6. Attach the standard caveat (Section 6).

---

## Section 3 — Control Crosswalk

Keyed by finding signal. Each signal maps to one or more controls across the three frameworks. Control IDs use the common published identifiers; confirm against the customer's audit scope.

### 3.1 Encryption (in transit / at rest)

| Finding signal (from chain) | SOC 2 (TSC) | PCI-DSS v4.0 | ISO 27001:2013 Annex A | Closed by (target state) |
|-----------------------------|-------------|--------------|------------------------|--------------------------|
| Database without TDE / unencrypted at rest | CC6.1 | Req 3 | A.10.1.1, A.18.1.3 | Aurora storage encryption (default KMS) |
| Unencrypted internal/service-to-service traffic | CC6.1, CC6.7 | Req 4 | A.13.1.1, A.14.1.3 | TLS 1.3 on ALB/EKS service mesh |
| Weak/deprecated crypto (from security scan) | CC6.1 | Req 4 | A.10.1.1 | Upgrade to .NET 8 crypto libs; ACM-managed certs |

### 3.2 Authentication & identity

| Finding signal | SOC 2 (TSC) | PCI-DSS v4.0 | ISO 27001 | Closed by |
|----------------|-------------|--------------|-----------|-----------|
| No MFA on admin/privileged access (ARA identity) | CC6.1, CC6.6 | Req 8 | A.9.4.2, A.9.2.3 | Cognito MFA / IdP-enforced MFA |
| Password-only or weak auth on sensitive app (ARA) | CC6.1 | Req 8 | A.9.4.3 | Cognito + OAuth2/OIDC |
| No machine/agent identity boundary (ARA) | CC6.1, CC6.3 | Req 7, Req 8 | A.9.2.1 | IAM roles + Cognito app clients |
| Hardcoded credentials/secrets (security scan) | CC6.1 | Req 8.3 | A.9.4.3, A.10.1.2 | AWS Secrets Manager + rotation |

### 3.3 Access control & review

| Finding signal | SOC 2 (TSC) | PCI-DSS v4.0 | ISO 27001 | Closed by |
|----------------|-------------|--------------|-----------|-----------|
| No least-privilege / broad access grants (MODA security baseline) | CC6.1, CC6.3 | Req 7 | A.9.1.1, A.9.2.3 | IAM least-privilege roles |
| No automated access review | CC6.2, CC6.3 | Req 7.2 | A.9.2.5 | IAM Access Analyzer |

### 3.4 Logging, monitoring & observability

| Finding signal | SOC 2 (TSC) | PCI-DSS v4.0 | ISO 27001 | Closed by |
|----------------|-------------|--------------|-----------|-----------|
| Unstructured/file-only logging (e.g. log4net to file) (ARA/MODA observability) | CC7.2 | Req 10 | A.12.4.1 | CloudWatch Logs (structured) |
| No centralized monitoring / no tracing | CC7.2, CC7.3 | Req 10.2 | A.12.4.1, A.16.1.2 | CloudWatch + X-Ray |
| No file integrity monitoring | CC7.1 | Req 11.5 | A.12.2.1 | AWS Config + CloudTrail |

### 3.5 Vulnerability & patch management

| Finding signal | SOC 2 (TSC) | PCI-DSS v4.0 | ISO 27001 | Closed by |
|----------------|-------------|--------------|-----------|-----------|
| Open CVEs in dependencies (security scan) | CC7.1 | Req 6.3 | A.12.6.1 | .NET 8 upgrade + dependency remediation |
| No automated vulnerability scanning | CC7.1 | Req 11.3 | A.12.6.1 | Amazon Inspector + ECR scanning |
| EOL framework/runtime (unpatched) | CC7.1 | Req 6.3.3 | A.12.6.1, A.14.2.2 | Modernize to supported .NET LTS |

### 3.6 Network segmentation

| Finding signal | SOC 2 (TSC) | PCI-DSS v4.0 | ISO 27001 | Closed by |
|----------------|-------------|--------------|-----------|-----------|
| Flat network / no segmentation (MODA security baseline) | CC6.6 | Req 1 | A.13.1.1, A.13.1.3 | VPC subnet tiering + security groups |

### 3.7 Change control

| Finding signal | SOC 2 (TSC) | PCI-DSS v4.0 | ISO 27001 | Closed by |
|----------------|-------------|--------------|-----------|-----------|
| Manual deployments / no CI/CD audit trail (MODA ops) | CC8.1 | Req 6.5 | A.14.2.2, A.12.1.2 | CodePipeline + change approval + CloudTrail |

---

## Section 4 — Post-Modernization Status Rules

- **✅ CLOSED** — the "Closed by" target control is part of the recommended target architecture *and* in a modernization wave. State the wave.
- **⚠️ PARTIAL** — target control is planned but retains a manual step (e.g. CI/CD manual approval satisfies control intent but is not fully automated).
- **⚪ DEFERRED** — resolution is post-EBA / out of current wave scope.

Do not mark a control CLOSED on the strength of the plan alone for audit purposes — that requires runtime evidence (Section 6).

---

## Section 5 — Output Format (Ch8 Compliance Gap Table)

Emit one row per (standard, control) gap. Columns follow the formatting standard for this framework:

| Standard | Control Area | Current Gap | Gap Detail | Severity | Post-Mod Status | Remediation Wave |
|----------|--------------|-------------|-----------|----------|-----------------|------------------|

Close with a one-line rollup: `N gaps today → M remaining post-modernization (X% uplift)`, and flag any control that remains a gap post-modernization prominently in the risks section.

---

## Section 6 — Post-Modernization Validation & Limitations

The crosswalk establishes the **pre-modernization** gap picture from code. Validating the **post-modernization "CLOSED" state** requires runtime evidence against the deployed AWS environment:

| Mechanism | Use | Caveat |
|-----------|-----|--------|
| AWS Audit Manager prebuilt frameworks (PCI DSS v4.0, SOC 2, ISO/IEC 27001:2013 Annex A, NIST 800-53, HIPAA) | Authoritative post-mod control validation | ⚠️ Audit Manager is **closed to new customers** — only usable by existing customers |
| AWS Security Hub standards (PCI DSS, CIS, AWS FSBP, NIST) | Fallback continuous control checks | Standard-specific coverage; not a 1:1 audit |
| AWS Config conformance packs | Fallback resource-level compliance rules | Requires Config enabled with the relevant packs |

**Mandatory caveat on every output:** "Compliance mapping is assessment-grade, derived from static code analysis. It indicates likely control gaps and does not constitute an audit or attestation. Formal compliance requires review by a qualified auditor and runtime evidence from the deployed environment."
