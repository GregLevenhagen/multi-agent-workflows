<!-- Demo Path: REVIEW REJECTION LOOP — triggers review issues on first pass (pricing inconsistency), routes back for correction, then passes on re-review -->

# Request for Proposal: Real-Time Data Analytics Platform

## Issuing Organization

**Globex International**
700 Financial District Drive
New York, NY 10004

**Contact:** James Chen, Chief Data Officer
**Email:** j.chen@globexintl.com
**Date Issued:** February 1, 2025
**Response Deadline:** March 15, 2025

---

## 1. Company Overview

Globex International is a mid-market financial technology company specializing in cross-border payment processing and multi-currency treasury management. Founded in 2012, we serve over 2,400 institutional clients including banks, broker-dealers, and corporate treasuries across 38 countries. Our platform processes approximately $18B in daily transaction volume, generating massive volumes of real-time data that are currently underutilized for analytics and decision support.

## 2. Project Background

As our transaction volumes have grown 340% over the past three years, our existing batch-based analytics infrastructure — built on nightly ETL jobs loading into an on-premises SQL Server data warehouse — can no longer meet business demands. Our risk management, compliance, and operations teams require real-time visibility into transaction patterns, anomaly detection, and regulatory reporting. Additionally, our clients are requesting self-service analytics dashboards showing their transaction flows and settlement status in real time.

We attempted an internal build using Apache Kafka and a custom Spark pipeline in Q3 2024, but the project stalled due to lack of specialized expertise in streaming architectures at scale. We are now seeking a consulting partner to design and deliver a production-grade solution.

## 3. Engagement Type

**Fixed Bid** — We require a complete, turnkey delivery of the analytics platform. The selected vendor will own the full lifecycle from design through deployment and hypercare. We expect a firm fixed price for the defined scope, with change orders managed through a formal process.

## 4. Scope of Work

### Phase 1: Discovery & Architecture (Months 1-2)
- Conduct stakeholder interviews with Risk, Compliance, Operations, and Client Services teams
- Analyze current data sources, schemas, and volumes (estimated 15TB/day ingest)
- Design target state architecture including data ingestion, transformation, storage, and visualization layers
- Produce Architecture Decision Records (ADRs) for all technology selections
- Deliver detailed project plan with resource allocation

### Phase 2: Data Ingestion & Stream Processing (Months 3-6)
- Implement Azure Event Hubs or Confluent Kafka for real-time data ingestion from 12+ source systems
- Build stream processing pipelines using Databricks Structured Streaming
- Implement data quality gates and schema validation
- Build dead-letter queue handling and replay mechanisms
- Deploy CDC (Change Data Capture) connectors for SQL Server and PostgreSQL source databases
- Target: Process 500K events/second with <2 second end-to-end latency

### Phase 3: Data Lake & Warehouse (Months 5-9)
- Implement medallion architecture (Bronze/Silver/Gold) on Azure Data Lake Storage Gen2
- Configure Databricks Unity Catalog for data governance and access control
- Build transformation layer using Databricks SQL and dbt
- Implement Snowflake as the serving layer for BI workloads
- Design and implement slowly changing dimension (SCD) patterns for client and currency reference data
- Build data models optimized for: transaction analysis, risk aggregation, regulatory reporting, client analytics

### Phase 4: Visualization & Self-Service (Months 7-11)
- Deploy Power BI Premium capacity with DirectQuery to Snowflake
- Build 8 pre-defined dashboards:
  1. Real-Time Transaction Monitor (Operations)
  2. Risk Exposure Dashboard (Risk Management)
  3. AML/KYC Alert Dashboard (Compliance)
  4. Settlement Status Tracker (Operations)
  5. Currency Flow Analysis (Treasury)
  6. Client Transaction Summary (Client Services)
  7. Revenue Analytics (Finance)
  8. Platform Health Monitor (Engineering)
- Implement row-level security for multi-tenant client access
- Build embedded analytics for client-facing portal using Power BI Embedded
- Create self-service dataset for ad-hoc analysis by business analysts

### Phase 5: ML Integration & Anomaly Detection (Months 9-13)
- **Critical dependency:** Requires Phase 3 Gold-layer datasets and Snowflake serving layer to be production-ready before ML training can begin
- **Note:** Our compliance team has mandated that anomaly detection must be operational by Month 11 at the latest to meet upcoming FinCEN regulatory deadlines — this may require starting ML model development in parallel with Phase 3 (Month 7) using synthetic/sample data, then retraining on production data once available
- Build real-time anomaly detection models using Databricks ML
- Implement transaction pattern classification for fraud screening
- Deploy model serving endpoints with <100ms inference latency
- Build A/B testing framework for model comparison
- Integrate ML predictions into Power BI dashboards as real-time KPIs

### Phase 6: Production Hardening & Hypercare (Months 14-18)
- Performance testing at 2x projected peak volume
- Disaster recovery configuration with <15 minute RPO, <1 hour RTO
- Security audit and penetration testing
- SOC 2 Type II compliance documentation
- Runbook creation for operations team
- Knowledge transfer sessions (minimum 40 hours)
- 90-day hypercare period with SLA-backed support

## 5. Technology Requirements

The following technologies are mandatory based on existing enterprise agreements:

| Layer | Technology | Notes |
|-------|-----------|-------|
| Stream Ingestion | Azure Event Hubs or Confluent Kafka | Must support at-least-once semantics |
| Stream Processing | Databricks Structured Streaming | Existing Databricks Enterprise license |
| Data Lake | Azure Data Lake Storage Gen2 | Medallion architecture required |
| Data Governance | Databricks Unity Catalog | Centralized access control |
| Data Warehouse | Snowflake Enterprise | Existing contract, East US 2 region |
| BI & Visualization | Power BI Premium | Existing M365 E5 license |
| ML Platform | Databricks ML | MLflow for model registry |
| Orchestration | Azure Data Factory or Databricks Workflows | Preference for Databricks Workflows |
| IaC | Terraform | Standard across all infrastructure |
| CI/CD | GitHub Actions | Enterprise GitHub license |

## 6. Team Expectations

We expect the vendor to provide a team including at minimum:
- 1 Solution Architect / Technical Lead
- 2 Senior Data Engineers (Databricks + Snowflake experience required)
- 1 Senior Analytics Engineer (dbt + Power BI)
- 1 ML Engineer (Databricks ML + real-time inference)
- 1 Project Manager (PMP or equivalent, financial services experience preferred)

## 7. Budget

Our approved budget range for this engagement is **$1,200,000 – $1,500,000**, structured as firm fixed-price with milestone-based payments tied to phase completion and acceptance criteria.

### Payment Schedule
- 15% upon contract execution
- 15% upon Phase 1 completion
- 20% upon Phase 2 completion
- 20% upon Phase 3 completion
- 15% upon Phase 4 completion
- 10% upon Phase 5 completion
- 5% upon final acceptance (end of hypercare)

### Preliminary Cost Allocation by Phase

For proposal evaluation purposes, we expect the cost distribution to approximately align with:

| Phase | Description | Estimated Cost |
|-------|-------------|---------------|
| Phase 1 | Discovery & Architecture | $150,000 |
| Phase 2 | Data Ingestion & Stream Processing | $350,000 |
| Phase 3 | Data Lake & Warehouse | $280,000 |
| Phase 4 | Visualization & Self-Service | $220,000 |
| Phase 5 | ML Integration & Anomaly Detection | $195,000 |
| Phase 6 | Production Hardening & Hypercare | $135,000 |
| **Total** | | **$1,300,000** |

*Note: These are target allocations. Proposals may redistribute across phases with justification.*

## 8. Timeline

- **Start Date:** May 1, 2025
- **End Date:** October 31, 2026
- **Duration:** 18 months

**Schedule Constraints:**
- Phase 4 (Visualization) requires Phase 2 streaming data and Phase 3 Snowflake models, but must begin Month 7 to deliver embedded analytics for a client-facing launch in Q1 2026
- Phase 5 (ML) must be operational by Month 11 per regulatory mandate, but depends on Phase 3 completion (Month 9) — only 2 months for ML development, testing, and deployment
- Proposals should address how these overlapping dependencies will be managed

## 9. Acceptance Criteria

Each phase must meet the following before payment release:
- All deliverables reviewed and approved by Globex technical lead
- Automated test suite with >80% code coverage for all pipelines
- Performance benchmarks met (documented in Phase 2 SLAs)
- Security review passed (no Critical or High findings unresolved)
- Documentation complete and reviewed

## 10. Key Risks & Constraints

- **Data sovereignty:** All data must remain in US-East Azure regions (regulatory requirement)
- **PII handling:** Transaction data contains PII subject to CCPA and GDPR for EU counterparties
- **Existing systems:** Source systems cannot be modified; all integration must be non-invasive
- **Availability:** Platform must achieve 99.9% uptime SLA during business hours (6am-8pm ET)
- **Vendor lock-in:** Architecture should minimize single-vendor dependencies where practical

## 11. Evaluation Criteria

| Criteria | Weight |
|----------|--------|
| Technical approach and architecture quality | 30% |
| Team experience with similar engagements in financial services | 25% |
| Demonstrated Databricks + Snowflake integration experience | 20% |
| Price and payment structure | 15% |
| Timeline and risk mitigation plan | 10% |

## 12. Submission Instructions

Submit proposals electronically to j.chen@globexintl.com and procurement@globexintl.com by March 15, 2025. Proposals must include: technical approach document, team resumes with relevant project experience, firm fixed-price breakdown by phase, and three client references in financial services.

Shortlisted vendors will be invited to a 90-minute technical presentation in our New York office.

---

*Globex International reserves the right to reject any and all proposals. Respondents bear all costs of proposal preparation. This RFP does not commit Globex to award a contract.*
