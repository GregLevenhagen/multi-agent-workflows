# Security Policy

## Supported Scope

Security reports are welcome for:

- Source code in this repository
- GitHub Actions and repository automation
- Infrastructure-as-code in `infra/`
- Sample data and configuration files if they create a security exposure

## Reporting a Vulnerability

Do not open a public issue for suspected vulnerabilities.

Use GitHub's private vulnerability reporting if it is enabled for the repository. If private reporting is unavailable, contact the maintainer directly through GitHub and include:

- A clear description of the issue
- The affected file, component, or workflow
- Reproduction steps or a proof of concept
- Impact assessment
- Any suggested remediation

## Response Expectations

This is a single-maintainer project. Response times are best-effort, but the target process is:

1. Acknowledge receipt within 7 days.
2. Validate and triage the report.
3. Prepare and release a fix when the issue is confirmed.
4. Credit the reporter if they want attribution.

## Disclosure

Please allow time for remediation before public disclosure.

## Operational Security Notes

- Secrets must never be committed to the repository.
- All pull requests are expected to pass CI and security checks.
- Dependency and code scanning alerts should be addressed before release where practical.
