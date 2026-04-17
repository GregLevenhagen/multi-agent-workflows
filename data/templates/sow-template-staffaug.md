<!--
  Template Placeholders ({{PLACEHOLDER}} syntax — agents fill these from pipeline data):
    Common:    CLIENT_NAME, ENGAGEMENT_ID, EFFECTIVE_DATE, COMPANY_NAME, EXECUTIVE_SUMMARY,
               SCOPE_DESCRIPTION, START_DATE, END_DATE, DURATION, TOTAL_PRICE, PRICING_TABLE
    StaffAug:  TEAM_TABLE, MILESTONES_TABLE
-->

# Statement of Work: Staff Augmentation Services

**Client:** {{CLIENT_NAME}}
**Engagement ID:** {{ENGAGEMENT_ID}}
**Effective Date:** {{EFFECTIVE_DATE}}
**Prepared By:** {{COMPANY_NAME}}

---

## 1. Executive Summary

{{EXECUTIVE_SUMMARY}}

## 2. Scope of Work

### 2.1 Engagement Overview
This Statement of Work defines the terms for staff augmentation services to be provided to {{CLIENT_NAME}}. The consulting team will embed within the client's existing organization, working under the client's day-to-day direction while providing specialized expertise.

### 2.2 Services
{{SCOPE_DESCRIPTION}}

### 2.3 Out of Scope
- Items not explicitly listed in Section 2.2
- Production support or on-call responsibilities unless separately agreed
- Third-party software licensing or infrastructure costs

## 3. Team Composition

| Role | Count | Key Skills | Allocation |
|------|-------|------------|------------|
{{TEAM_TABLE}}

### 3.1 Staffing Commitments
- All consultants will be available during client's standard business hours
- Replacement consultants will be provided within 10 business days if a team member departs
- Client retains right to request consultant replacement with 5 business days notice

## 4. Timeline

- **Start Date:** {{START_DATE}}
- **End Date:** {{END_DATE}}
- **Duration:** {{DURATION}}

### 4.1 Milestones
{{MILESTONES_TABLE}}

### 4.2 Working Arrangements
- On-site presence as specified in the engagement agreement
- Remote work permitted for remaining days with client approval
- Standard working hours: 8 hours/day, 40 hours/week per consultant

## 5. Pricing

### 5.1 Rate Card
| Role | Hourly Rate | Estimated Monthly Hours | Monthly Estimate |
|------|------------|------------------------|-----------------|
{{PRICING_TABLE}}

### 5.2 Billing Terms
- Invoiced monthly based on actual hours worked
- Timesheets submitted weekly, approved by client project manager
- Net 30 payment terms from invoice date
- Travel and expenses billed at cost with prior approval

### 5.3 Total Estimated Value
**{{TOTAL_PRICE}}** over {{DURATION}} (estimate based on full utilization)

## 6. Terms and Conditions

### 6.1 Governance
- Weekly status meetings with client project manager
- Monthly executive review with steering committee
- Change requests processed within 5 business days

### 6.2 Intellectual Property
All work product created during the engagement is owned by {{CLIENT_NAME}}.

### 6.3 Confidentiality
Subject to the Mutual Non-Disclosure Agreement executed between the parties.

### 6.4 Termination
Either party may terminate with 30 days written notice. Client is responsible for hours worked through the termination effective date.

{{#IF_MANUFACTURING}}
## 7. Manufacturing & Industrial Addendum

### 7.1 On-Site Requirements
- Consultants must comply with Client's facility safety protocols including PPE requirements
- Background checks and facility clearance required before on-site access
- Consultants must complete Client's safety orientation prior to first facility visit

### 7.2 Compliance
- All work touching production systems must comply with Client's quality management system (ISO 9001/AS9100 where applicable)
- Change management processes must align with Client's existing ITSM framework
{{/IF_MANUFACTURING}}

{{#IF_CLOUD_MIGRATION}}
## 7. Cloud Migration Addendum

### 7.1 Migration Standards
- All migrations must follow the Azure Well-Architected Framework principles
- Zero-downtime migration is required for all production workloads
- Rollback procedures must be documented and tested for each migration phase
- Data migration validation must include row count reconciliation and data integrity checks

### 7.2 Security & Compliance Continuity
- Existing compliance certifications (SOC 2, ISO 27001, etc.) must not be disrupted during migration
- Network security posture must be maintained or improved throughout the migration
- All cloud resources must be provisioned using infrastructure-as-code (IaC) for repeatability
{{/IF_CLOUD_MIGRATION}}

---

**Accepted By:**

___________________________ | ___________________________
{{CLIENT_NAME}} | {{COMPANY_NAME}}
Date: _________________ | Date: _________________
