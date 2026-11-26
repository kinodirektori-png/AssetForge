# 0006. Model-neutral generator, FLUX.2 [pro] on a preview API in the interim

- Status: Accepted
- Date: 2026-11-26
- Source: Owner decision
- Related: spec §4, §8; C-3, DEC-3, DEC-9; issue #7, #12

## Context

MAI-Image models are not available in the owner's Azure region (Spain Central), and gpt-image is not in the Foundry
list, so the spike used FLUX.2 [pro] (DEC-9). The final model is chosen after the spike. FLUX.2 [pro] is GA, but its
Foundry route only offers `api-version=preview`, while DEC-3 says GA only.

## Decision

- The app generates through a model-neutral `IImageGenerator`. Nothing outside Infrastructure knows the model.
- Until the model decision, the implementation is the spike's FLUX.2 [pro] client:
  `POST {endpoint}/providers/blackforestlabs/v1/{deployment}?api-version=preview` with a Bearer key.
- Using `api-version=preview` is an **accepted departure from DEC-3** for the MVP, reviewed when DEC-9 is decided.
- Model, deployment and API version come from configuration and are stored with each image.

## Options considered

- **Wait for the model decision.** Blocks the MVP.
- **Build directly against FLUX.** Makes a later model change touch the whole app.

## Consequences

- The response shape of the preview route is undocumented; the client is tested against the shape recorded in the spike
  notes.
- A preview API can change without notice; the stored API version shows which images it produced.
