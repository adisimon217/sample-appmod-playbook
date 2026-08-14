# .NET & SQL Server Modernization — EBA Preparation Checklist

> **Purpose:** Step-by-step preparation guide for the 2-Day Experience-Based Acceleration (EBA).
>
> **Target State:** .NET application on Amazon ECS Fargate (Linux containers) + SQL Server migrated to Amazon Aurora PostgreSQL.
>
> **Prerequisite:** Complete Phase 300 (detailed assessment + architecture) before starting this checklist. The consolidated assessment and architecture working docs should be finalized with all decisions 🟢 AGREED or ⚪ DEFERRED.

---

## Phase 1 — Application Data Gathering

This is the first activity. The customer team completes the questionnaire using the tools referenced in its appendices.

- [ ] Questionnaire completed for each application in scope
- [ ] Questionnaire responses validated by the application SME
- [ ] Source code access granted (repo cloned or zip provided)
- [ ] Lines-of-code metrics collected (see Questionnaire Appendix)
- [ ] .NET compatibility assessment run via [AWS Transform for .NET](https://docs.aws.amazon.com/transform/latest/userguide/dotnet.html) (see Questionnaire Appendix)
- [ ] SQL Server Enterprise feature check run (see Questionnaire Appendix)
- [ ] AWS SCT or DMS Schema Conversion assessment report generated (see Questionnaire Appendix)

---

## Phase 2 — Environment & Access Setup

- [ ] AWS account provisioned with necessary permissions (ECS, ECR, Aurora, VPC, IAM, DMS)
- [ ] Source code repository access granted to the EBA team
- [ ] Database credentials for a non-production environment
- [ ] VPN / network connectivity to source systems confirmed
- [ ] Docker Desktop installed on development laptops
- [ ] Isolated sandbox environment confirmed — full application stack (app + DB + dependencies) can be loaded and run independently (see Questionnaire 7.7)
- [ ] Mock interfaces or stubs available for any dependencies that cannot be sandboxed
- [ ] .NET SDK (target version) installed
- [ ] IDE installed (Visual Studio / [Kiro](https://kiro.dev/docs/))
- [ ] AWS CLI and AWS Toolkit configured

---

## Phase 3 — People & Logistics

- [ ] SMEs confirmed and calendars blocked for the full 2 days:
  - Application developer(s) who know the codebase
  - DBA / database administrator
  - Infrastructure / DevOps engineer
  - Business owner / product owner (available for questions)
- [ ] War room booked (physical or virtual with screen sharing)
- [ ] Application architecture diagram prepared (even a whiteboard sketch)
- [ ] List of known pain points, risks, or blockers documented
- [ ] Non-production environment available for testing during the EBA
- [ ] Can non-production environments be suspended evenings/weekends? (cost optimization)

---

## Phase 4 — EBA Day Readiness

- [ ] Questionnaire responses reviewed by the AWS team
- [ ] SCT assessment report reviewed — key action items identified
- [ ] AWS Transform for .NET assessment completed
- [ ] AWS Transform for SQL Server Modernization job scoped (if applicable)
- [ ] Target architecture draft prepared (ECS Fargate + Aurora PostgreSQL)
- [ ] Migration approach agreed (big bang vs. phased, strangler fig, etc.)
- [ ] Success criteria defined for the 2-day sprint
