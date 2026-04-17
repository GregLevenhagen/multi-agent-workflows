<!--
  Template Placeholders ({{PLACEHOLDER}} syntax — agents fill these from pipeline data):
    Common:    CLIENT_NAME, ENGAGEMENT_ID, EFFECTIVE_DATE, COMPANY_NAME, EXECUTIVE_SUMMARY,
               SCOPE_DESCRIPTION, START_DATE, END_DATE, DURATION, TOTAL_PRICE, PRICING_TABLE
    FixedBid:  IN_SCOPE_ITEMS, ASSUMPTIONS, DELIVERABLES_TABLE, MILESTONES_TABLE,
               ACCEPTANCE_CRITERIA, PAYMENT_SCHEDULE, DEPENDENCIES, RISK_MITIGATION, TEAM_TABLE

  Conditional Sections ({{#IF_INDUSTRY}} ... {{/IF_INDUSTRY}} — include/omit based on client industry):
    IF_FINTECH:    Financial services regulatory addendum (SOC 2, PCI DSS, CCPA/GDPR, data sovereignty)
    IF_HEALTHCARE: Healthcare compliance addendum (HIPAA BAA, PHI encryption, audit trails)
    Agents should include the relevant conditional section and remove the markers, or remove the entire
    conditional block if not applicable to the client's industry.
-->

# Statement of Work: Fixed-Bid Engagement

**Client:** {{CLIENT_NAME}}
**Engagement ID:** {{ENGAGEMENT_ID}}
**Effective Date:** {{EFFECTIVE_DATE}}
**Prepared By:** {{COMPANY_NAME}}

---

## 1. Executive Summary

{{EXECUTIVE_SUMMARY}}

## 2. Scope of Work

### 2.1 Project Overview
{{SCOPE_DESCRIPTION}}

### 2.2 In Scope
{{IN_SCOPE_ITEMS}}

### 2.3 Out of Scope
- Items not explicitly listed in Section 2.2
- Changes to source systems or third-party platforms
- Ongoing maintenance and support beyond the hypercare period
- Third-party software licensing or infrastructure costs

### 2.4 Assumptions
{{ASSUMPTIONS}}

## 3. Deliverables

| # | Deliverable | Description | Due Date | Acceptance Criteria |
|---|------------|-------------|----------|-------------------|
{{DELIVERABLES_TABLE}}

### 3.1 Acceptance Process
- Deliverables submitted for client review upon completion
- Client has 5 business days to review and provide feedback
- Feedback addressed within 3 business days
- Formal acceptance signoff required before phase payment release

## 4. Milestones

| # | Milestone | Target Date | Payment % |
|---|-----------|-------------|-----------|
{{MILESTONES_TABLE}}

## 5. Acceptance Criteria

Each milestone must meet the following before payment release:
{{ACCEPTANCE_CRITERIA}}

## 6. Pricing

### 6.1 Fixed Price
**Total Project Price: {{TOTAL_PRICE}}**

### 6.2 Price Breakdown by Phase
| Phase | Scope Summary | Price |
|-------|--------------|-------|
{{PRICING_TABLE}}

### 6.3 Payment Schedule
{{PAYMENT_SCHEDULE}}

### 6.4 Change Orders
- Scope changes require a formal Change Order signed by both parties
- Change Orders include: description, impact assessment, price adjustment, timeline impact
- No work begins on changes until Change Order is fully executed

## 7. Timeline

- **Start Date:** {{START_DATE}}
- **End Date:** {{END_DATE}}
- **Duration:** {{DURATION}}

### 7.1 Key Dependencies
{{DEPENDENCIES}}

### 7.2 Risk Mitigation
{{RISK_MITIGATION}}

## 8. Team

### 8.1 Vendor Team
| Role | Responsibilities |
|------|-----------------|
{{TEAM_TABLE}}

### 8.2 Client Responsibilities
- Timely access to stakeholders, systems, and data
- Review and feedback within agreed timelines
- Decision-making authority for acceptance signoff
- Designated project manager as single point of contact

## 9. Terms and Conditions

### 9.1 Governance
- Bi-weekly status reports and steering committee updates
- Monthly executive review meetings
- Risk and issue escalation within 24 hours

### 9.2 Intellectual Property
All deliverables and work product are owned by {{CLIENT_NAME}} upon final payment.

### 9.3 Warranty
Vendor provides a 90-day warranty on all deliverables from final acceptance. Defects in delivered functionality will be corrected at no additional cost.

### 9.4 Limitation of Liability
Total liability shall not exceed the total contract value.

### 9.5 Termination
- Client may terminate for convenience with 30 days notice; payment due for completed milestones
- Either party may terminate for cause with 15 days notice and opportunity to cure

{{#IF_FINTECH}}
## 10. Financial Services Addendum

### 10.1 Regulatory Compliance
- All deliverables shall comply with applicable financial regulations including but not limited to SOC 2, PCI DSS, CCPA, and GDPR where EU data subjects are involved
- Consultant personnel with access to production systems shall undergo background checks
- All code changes to systems processing financial data require dual review and sign-off

### 10.2 Data Sovereignty
- Client data shall not leave the geographic regions specified in the engagement scope
- All development, testing, and production environments shall reside within approved regions
- Consultant shall maintain data residency certification documentation upon request

### 10.3 Audit Rights
- Client and its regulators retain the right to audit Consultant's security controls, processes, and systems with 10 business days notice
- Consultant shall provide SOC 2 Type II report or equivalent upon request
{{/IF_FINTECH}}

{{#IF_HEALTHCARE}}
## 10. Healthcare Compliance Addendum

### 10.1 HIPAA Compliance
- Consultant shall execute a Business Associate Agreement (BAA) prior to accessing any Protected Health Information (PHI)
- All systems processing PHI shall implement encryption at rest (AES-256) and in transit (TLS 1.2+)
- Consultant personnel shall complete HIPAA training prior to engagement start

### 10.2 Audit and Access Controls
- Role-based access controls (RBAC) shall be implemented for all systems containing PHI
- Audit logs shall be retained for minimum 6 years per HIPAA requirements
- Breach notification shall follow HIPAA timelines (60 days for affected individuals)
{{/IF_HEALTHCARE}}

---

**Accepted By:**

___________________________ | ___________________________
{{CLIENT_NAME}} | {{COMPANY_NAME}}
Date: _________________ | Date: _________________
