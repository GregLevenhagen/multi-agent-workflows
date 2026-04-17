# Contributing

Thanks for your interest in improving this repository.

## Before You Start

- Read the [README](README.md) to understand the project scope and setup.
- Use [issues](https://github.com/greglevenhagen/multi-agent-workflows/issues) for bugs, scoped feature proposals, and documentation problems.
- Follow [SECURITY.md](SECURITY.md) for sensitive reports. Do not file public issues for vulnerabilities.
- Expect maintainer-led review and prioritization. This is a single-maintainer project.

## Development Setup

```bash
git clone https://github.com/<your-username>/multi-agent-workflows.git
cd multi-agent-workflows
dotnet restore src/SalesToSignature.sln
dotnet build src/SalesToSignature.sln --configuration Release
dotnet test src/SalesToSignature.sln --configuration Release
```

If you need runtime configuration, copy `.env.example` to `.env` and provide your own Azure values.

## Contribution Workflow

1. Fork the repository.
2. Create a focused branch from `main`.
3. Make the smallest change that solves the problem cleanly.
4. Add or update tests when behavior changes.
5. Run the local validation commands before opening a pull request.
6. Open a pull request with a clear summary, validation notes, and any relevant issue links.

Direct pushes to `main` are not part of the normal contribution flow.

## Local Validation

Run these commands before submitting a pull request:

```bash
dotnet build src/SalesToSignature.sln --configuration Release
dotnet test src/SalesToSignature.sln --configuration Release
```

If your change affects docs, examples, or deployment instructions, update the relevant files in the same pull request.

## Code Expectations

- Follow the repository `.editorconfig`.
- Keep code and tests readable over clever.
- Prefer small, composable classes and explicit naming.
- Keep public API and behavior changes documented.
- Do not commit secrets, credentials, or local environment files.

## Commit Messages

Conventional Commits are preferred:

- `feat:` for new functionality
- `fix:` for bug fixes
- `docs:` for documentation changes
- `test:` for test changes
- `refactor:` for refactoring
- `chore:` for maintenance work

## Pull Request Expectations

- Keep pull requests focused and reviewable.
- Explain what changed and why.
- Call out breaking changes explicitly.
- Ensure GitHub Actions pass before requesting merge.
- Be prepared for feedback on scope, maintainability, or project fit.

## What May Be Declined

The maintainer may decline contributions that:

- Expand the project beyond its intended reference scope
- Add significant maintenance burden without enough payoff
- Introduce unnecessary dependencies or operational complexity
- Lack tests or clear validation for behavior changes
