# Architecture decision records

Each record is one decision: the context, what was decided, the options weighed, and what follows from it. Records are
never rewritten after they are accepted. A changed decision gets a new record that supersedes the old one.

Product decisions (DEC-1 to DEC-9) live in [requirements.md §13](../requirements.md#13-decisions). These records cover
the MVP spec ([spec.md](../spec.md)) and the review of its implementation, and point to the DEC items they build on.

"Owner decision" means the project owner chose it explicitly. "Approved with the spec" means it was proposed in the spec
and accepted when the owner approved the spec on 2026-11-26.

| # | Decision | Status | Source |
|---|---|---|---|
| [0001](0001-dotnet-10-minimal-api-layers.md) | .NET 10 Web API with minimal APIs in three layers | Accepted | Owner decision |
| [0002](0002-sqlite-ef-core-local-data.md) | SQLite through EF Core, data in the user profile | Accepted | Owner decision |
| [0003](0003-scalar-openapi-ui.md) | Scalar as the OpenAPI UI, served locally | Accepted | Approved with the spec |
| [0004](0004-local-only-access-protection.md) | Layered local-only access protection | Accepted | Owner decision |
| [0005](0005-foundry-api-key-auth.md) | Foundry API key from user-secrets for the MVP | Accepted | Owner decision |
| [0006](0006-model-neutral-generator-interim-flux.md) | Model-neutral generator, FLUX.2 [pro] on a preview API in the interim | Accepted | Owner decision |
| [0007](0007-per-model-limits-and-pricing-in-config.md) | Per-model limits and pricing as configuration | Accepted | Owner decision |
| [0008](0008-generation-never-loses-paid-images.md) | Generation runs to completion and stores each variant at once | Accepted | Owner decision |
| [0009](0009-budget-reservation.md) | Budget reserved before a round, settled per call | Accepted | Approved with the spec |
| [0010](0010-style-guide-copy-per-request.md) | A style guide copy on each request instead of versions | Accepted | Owner decision |
| [0011](0011-export-under-configured-root.md) | Export only under a configured root | Accepted | Owner decision |
| [0012](0012-coverage-gate-as-separate-check.md) | Coverage gate as a separate required CI check | Accepted | Owner decision |
| [0013](0013-nuget-review-per-pr.md) | NuGet packages reviewed in the PR that adds them | Accepted | Owner decision |

## Template

```
# NNNN. Title

- Status: Proposed | Accepted | Superseded by NNNN
- Date: YYYY-MM-DD
- Source: Owner decision | Approved with the spec
- Related: spec §, FR-, DEC-

## Context
## Decision
## Options considered
## Consequences
```
