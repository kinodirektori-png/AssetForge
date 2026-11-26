# 0013. NuGet packages reviewed in the PR that adds them

- Status: Accepted
- Date: 2026-11-26
- Related: spec §8

## Context

Every package is code from outside the project that runs with the user's Azure key and file system access, a licence to
comply with, and a dependency to keep updated. Packages added in bulk are easy to wave through.

## Decision

- Each PR lists the packages it adds, with version, project, reason and licence, so each one is judged together with
  the code that needs it.
- The packages planned for the MVP are listed in spec §8. Prefer the .NET platform and Microsoft packages; add a third
  party package only when it saves real work.

## Consequences

- A package not on the list needs explicit agreement before it is used.
- The dependency list stays short, which keeps upgrades and licence checks cheap.
