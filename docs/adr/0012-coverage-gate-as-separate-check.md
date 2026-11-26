# 0012. Coverage gate as a separate required CI check

- Status: Accepted
- Date: 2026-11-26
- Related: spec §2 item 11; claude.md (80 % coverage)

## Context

The repository requires at least 80 % line coverage. `coverlet.collector` only writes a report, so a script had to read
it and fail CI. Mixing coverage into the build check also hides the real signal: a red build should mean the code does
not compile or a test fails, not that coverage dropped. Low coverage should block the merge, not the build.

## Decision

- Coverage uses **coverlet.msbuild** (MIT) with its built-in threshold: `dotnet test -p:CollectCoverage=true` fails below
  80 % total line coverage. No scripts.
- CI has separate checks: "Build and test" on Linux and Windows, and "Coverage (80 % lines)".
- A branch rule on `Main` requires these checks, so a PR under 80 % cannot be merged.
- Before opening a PR, the same command is run locally.

## Options considered

- **Keep the scripts.** Extra code to maintain in two languages, and Python is not available on every dev machine.
- **A third-party GitHub Action reading the report.** CI only, no local check, an extra dependency.

## Consequences

- The branch rule must name the CI checks exactly; renaming a job means updating the rule.
