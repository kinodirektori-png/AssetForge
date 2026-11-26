# 0007. Per-model limits and pricing as configuration

- Status: Accepted
- Date: 2026-11-26
- Source: Owner decision (plan review)
- Related: spec §4; NFR-9, FR-11, OQ-6; 0006

## Context

The first plan hard-coded MAI-Image size limits and one price per megapixel. FLUX.2 [pro] has different limits (at most
4 MP, prompt up to 32,000 tokens) and a tiered price: $0.03 for the first megapixel and $0.015 for each further one.

## Decision

- Each model's settings hold its size limits (smallest side, largest total), maximum prompt length and pricing rule.
- Requested sizes are checked against the limits at startup and before every call.
- The USD→EUR rate is configured. The price and rate used are stored with each call.

## Options considered

- **Constants in code.** Every model change would need a code change and would be easy to get wrong.

## Consequences

- Swapping the model is mostly configuration.
- The pricing rule needs a small format that can express tiers; it is reviewed with issue #8.
