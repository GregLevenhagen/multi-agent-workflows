<!-- Demo Path: NO-GO — triggers qualification rejection due to unrealistic timeline, absent CTO, vague scope, limited stakeholder access, and signs of organizational dysfunction -->

# Request for Proposal: Digital Transformation Assessment

## Issuing Organization

**Initech LLC**
42 Startup Lane
Austin, TX 78701

**Contact:** Tom Benson, CEO
**Email:** tom@initech.io
**Date Issued:** February 10, 2025
**Response Deadline:** February 24, 2025

---

## 1. Company Overview

Initech LLC is a 50-person startup founded in 2021, focused on HR technology for mid-market companies. We have raised a $12M Series A and are growing rapidly. Our engineering team of 18 developers maintains a monolithic Ruby on Rails application deployed on Heroku, along with several microservices in various stages of completion. We've gone through two engineering reorganizations in the past 9 months to try to improve velocity, and recently parted ways with our VP of Engineering over strategic disagreements. We believe we need to "go to the next level" technologically but aren't entirely sure what that means for us specifically.

## 2. Project Background

Our board of directors has mandated a "digital transformation" initiative following feedback from several enterprise prospects who expressed concerns about our technology stack's scalability and security posture during due diligence. We've also been experiencing increasing deployment failures, slow page loads, and occasional downtime that our team has been unable to fully diagnose.

Honestly, we're not sure if we need to replatform, refactor, or just optimize what we have. We previously engaged a boutique consulting firm for a similar assessment in Q2 2024, but the engagement ended early due to "misaligned expectations" — we'd rather not go into the details. We'd like an outside perspective to help us figure out the right path forward.

## 3. Engagement Type

We're flexible on engagement structure — probably **advisory or time-and-materials** would make the most sense. We need someone to come in, look at everything, and tell us what to do. Happy to discuss the best approach.

## 4. What We Need

We're looking for a consulting firm to help us with some or all of the following (we're open to suggestions on what's most important):

- Review our current architecture and technology stack
- Assess our cloud infrastructure and recommend improvements
- Look at our security practices (we think they're okay but haven't had an audit)
- Help us understand if we should stay on Heroku or move to AWS/Azure
- Maybe help us think about our data strategy?
- Give us a roadmap for the next 12-18 months
- Possibly help with hiring — we're not sure if we have the right team composition
- Some of our enterprise prospects have mentioned SOC 2 — we might need to deal with that at some point

We want this done quickly because our next board meeting is in **3 weeks** and we need to present a technology roadmap. Ideally the assessment would be complete before then.

## 5. Technology Stack

- **Backend:** Ruby on Rails 6.1 (monolith), 2 Node.js microservices, 1 Python ML service
- **Frontend:** React 17 (embedded in Rails via Webpacker)
- **Database:** PostgreSQL 13 on Heroku Postgres
- **Hosting:** Heroku (Professional dynos)
- **CI/CD:** GitHub Actions (basic setup)
- **Monitoring:** New Relic (free tier), Sentry for error tracking
- Other stuff we've probably forgotten about

### Known Technical Issues (from our internal retrospectives)
- The Rails monolith has grown to ~180K lines of Ruby with no automated test suite — deployment confidence is low
- Several controllers exceed 1,500 lines; the main `PayrollController` is ~3,200 lines with deeply nested conditionals
- The two Node.js microservices were built by a contractor who is no longer available; documentation consists of a sparse README
- Database migrations have been applied manually in production on at least two occasions — migration history may not match actual schema
- Webpacker configuration was customized heavily and nobody on the current team understands it; upgrading React or Webpack has been deemed "too risky"
- The Python ML service uses pinned dependencies from 2022 with known CVEs (flagged by GitHub Dependabot but not addressed)
- We recently discovered hardcoded API keys in three different config files committed to the repository

## 6. Budget

Our budget for this engagement is **$200,000 – $350,000**. We could potentially go higher if the scope warrants it, but we'd need to get additional board approval which takes time.

## 7. Timeline

- **Desired Start:** ASAP (next week if possible)
- **Assessment Complete:** 3 weeks from start
- **Board Presentation:** March 5, 2025

We understand this is aggressive but it's driven by our board meeting schedule. We're willing to be flexible on scope if needed to hit this timeline.

## 8. Team Access

We can make the following people available for interviews and working sessions:
- Tom Benson, CEO (limited availability — 2 hours/week max)
- Engineering team leads (rotating availability)
- Our DevOps person (they're also a developer, so time is limited)

We don't have a CTO currently — Tom has been filling that role informally. We've also had some turnover on the engineering team lately (4 departures in the past quarter), so institutional knowledge is a concern.

## 9. Deliverables

- Technology assessment report
- Architecture recommendations
- Roadmap presentation for the board
- Anything else you think we need

## 10. Evaluation

We'll pick whoever seems like the best fit. Please just send us something that shows you understand our situation and can help.

---

*Please reply to tom@initech.io with questions or your proposal.*
