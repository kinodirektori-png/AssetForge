# 0013. NuGet packages reviewed in the PR that adds them

- Status: Accepted
- Date: 2026-11-26
- Source: Owner decision
- Related: spec §8

## Context

Every package is a dependency to maintain and a licence to accept. The owner wants to see each one.

## Decision

- No package is added without asking. Each PR lists the packages it adds, with version, project, reason and licence.
- The packages planned for the MVP are listed in spec §8.

## Consequences

- A package not on the list needs the owner's go before it is used.
