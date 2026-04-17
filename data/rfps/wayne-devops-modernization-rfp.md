<!-- Demo Path: HAPPY PATH (TimeAndMaterials variant) — exercises the T&M engagement type through Intake → Qualification (Go) → Proposal → Contract → Review (Clean) → Approval -->

# Request for Proposal: DevOps & Platform Engineering Modernization

## Issuing Organization

**Wayne Industries**
1007 Mountain Drive, Suite 200
Gotham City, NJ 07101

**Contact:** Lucius Fox, SVP of Technology
**Email:** l.fox@wayneindustries.com
**Date Issued:** March 1, 2025
**Response Deadline:** April 15, 2025

---

## 1. Company Overview

Wayne Industries is a diversified industrial conglomerate with $8.7B in annual revenue, operating across defense technology, green energy, healthcare devices, and advanced materials. Our central IT organization supports 22,000 employees and manages over 60 production applications across Azure, AWS, and on-premises data centers. We have a mature engineering organization of 140 developers and 18 platform engineers.

## 2. Project Background

Over the past 5 years, our DevOps practices have evolved organically across business units, resulting in inconsistent tooling, fragmented CI/CD pipelines, and siloed platform knowledge. We currently maintain 47 separate Azure DevOps projects, 12 Jenkins instances, and 8 GitHub Enterprise organizations — with no unified standards for deployment, monitoring, or incident response.

Our newly formed Platform Engineering team has been tasked with building an Internal Developer Platform (IDP) to standardize the developer experience across the organization. We have the vision and executive sponsorship but need consulting expertise to accelerate the implementation and bring proven patterns from similar enterprise transformations.

## 3. Engagement Type

**Time & Materials** — Given the evolving nature of platform engineering work and the need for flexibility as we discover organizational constraints, we prefer a T&M engagement. We expect the scope to evolve as we learn what works for our engineering culture. Monthly burn rate should be predictable, and we'll conduct monthly scope reviews with a 2-week termination notice period.

## 4. Scope of Work

### Stream 1: Developer Platform Foundation (Months 1-4)
- Evaluate and select IDP tooling (Backstage, Port, or custom solution)
- Design golden path templates for our top 5 application archetypes (.NET API, React SPA, Python data service, Terraform module, mobile backend)
- Implement self-service project scaffolding with built-in compliance guardrails
- Build standardized CI/CD pipeline templates for GitHub Actions
- Integrate with existing Azure AD (Entra ID) for RBAC

### Stream 2: Infrastructure Standardization (Months 2-6)
- Consolidate IaC to Terraform with a module registry
- Implement Terraform Cloud workspaces with Sentinel policy-as-code
- Design environment promotion strategy (dev → staging → production)
- Build Kubernetes cluster standards (AKS) with Flux CD for GitOps
- Implement cost tagging and FinOps dashboards

### Stream 3: Observability & Reliability (Months 3-8)
- Deploy unified observability stack (OpenTelemetry, Grafana, Prometheus)
- Define and implement SLI/SLO framework across critical services
- Build automated incident response runbooks
- Implement chaos engineering practices with Gremlin or Litmus
- Establish on-call rotation tooling and escalation policies

### Stream 4: Developer Experience & Adoption (Months 4-10)
- Build internal documentation portal with API catalog
- Create developer onboarding automation (environment setup in <30 minutes)
- Implement developer satisfaction surveys and feedback loops
- Run workshop series for engineering teams (target 80% adoption by Month 8)
- Establish platform engineering office hours and support model

## 5. Technology Stack

| Area | Current State | Target State |
|------|--------------|--------------|
| Source Control | GitHub Enterprise (fragmented) | GitHub Enterprise (consolidated) |
| CI/CD | Azure DevOps + Jenkins + GitHub Actions | GitHub Actions (standardized) |
| IaC | Mix of ARM, Bicep, Terraform, manual | Terraform (standardized) |
| Containers | AKS (3 clusters) + Docker Compose | AKS (consolidated) + Flux CD |
| Monitoring | Datadog (partial) + Azure Monitor + custom | OpenTelemetry + Grafana + Prometheus |
| Secrets | Azure Key Vault + HashiCorp Vault + .env files | HashiCorp Vault (standardized) |
| IDP | None | Backstage or equivalent |

## 6. Team Requirements

We expect the following consultants, with flexibility to scale up or down monthly:

| Role | Count | Duration | Key Skills |
|------|-------|----------|------------|
| Platform Architect | 1 | 10 months | IDP design, Backstage, enterprise DevOps |
| Senior DevOps Engineer | 2 | 8 months | Terraform, AKS, GitHub Actions, GitOps |
| SRE / Observability Lead | 1 | 6 months | OpenTelemetry, Grafana, SLO frameworks |
| Project Manager | 1 | 10 months | Agile, stakeholder management, enterprise change |

All consultants must be US-based and available for on-site sessions in Gotham City one week per month.

## 7. Budget

Our approved budget for this engagement is **$800,000 – $1,100,000** on a time-and-materials basis.

- Expected monthly burn rate: $80,000 – $120,000
- Monthly invoicing with Net 30 payment terms
- Monthly scope reviews with adjustment authority up to ±15% without change order
- 2-week notice period for engagement wind-down

## 8. Timeline

- **Start Date:** May 15, 2025
- **End Date:** March 15, 2026
- **Duration:** 10 months

### Quarterly Milestones
1. **Q2 2025 (Months 1-2):** Platform foundation selected and CI/CD templates operational
2. **Q3 2025 (Months 3-5):** IaC consolidated, observability stack deployed, first teams onboarded
3. **Q4 2025 (Months 6-8):** 50% team adoption, SLO framework operational, chaos engineering started
4. **Q1 2026 (Months 9-10):** 80% adoption, platform self-sustaining, knowledge transfer complete

## 9. Evaluation Criteria

| Criteria | Weight |
|----------|--------|
| Platform engineering experience at enterprise scale (1000+ developers) | 30% |
| Demonstrated Backstage / IDP implementation experience | 20% |
| Team qualifications and relevant certifications | 20% |
| Cultural fit and collaboration approach | 15% |
| Rate card and estimated total cost | 15% |

## 10. Submission Instructions

Please submit your proposal to l.fox@wayneindustries.com by April 15, 2025. Proposals should include: team resumes, 2-3 case studies of similar engagements, proposed approach with phasing, rate card by role, and estimated monthly/total cost.

We plan to invite 2-3 finalists for in-person presentations at our Gotham City headquarters.

---

*Wayne Industries reserves the right to reject any and all proposals. This RFP does not constitute a commitment to award a contract or reimburse proposal preparation costs.*
