# 0007. Per-model limits and pricing as configuration

- Status: Accepted
- Date: 2026-11-26
- Related: spec §4; NFR-9, FR-11, OQ-6; 0006

## Context

The model will change after the spike (0006), and models differ in exactly the numbers the app depends on. MAI-Image
has a minimum side and a pixel cap; FLUX.2 [pro] allows up to 4 MP and 32,000 prompt tokens, and is priced in tiers:
$0.03 for the first megapixel and $0.015 for each further one. A single hard-coded price per megapixel cannot express
that, and wrong limits or prices mean failed calls or a budget cap that does not hold.

## Decision

- Each model's settings hold its size limits (smallest side, largest total), maximum prompt length and pricing rule.
- Requested sizes are checked against the limits at startup and before every call.
- The USD→EUR rate is configured. The price and rate used are stored with each call.

## Options considered

- **Constants in code.** Every model change would need a code change and would be easy to get wrong.

## Consequences

- Swapping the model is mostly configuration.
- The pricing rule needs a small format that can express tiers; it is reviewed with issue #8.
