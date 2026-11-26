# 0005. Foundry API key from user-secrets for the MVP

- Status: Accepted
- Date: 2026-11-26
- Related: spec §4, §8; NFR-5

## Context

The app calls an Azure Foundry image model. Foundry accepts an API key or Microsoft Entra ID. The requirements prefer
the owner's Azure sign-in (NFR-5), which needs more setup.

## Decision

For the MVP, use the **API key**, stored in user-secrets (`Foundry:Endpoint`, `Foundry:ApiKey`). It is never committed,
logged or printed. Entra ID sign-in comes later.

## Options considered

- **Entra ID now** (`DefaultAzureCredential`). Matches NFR-5, but adds a package, role assignments and sign-in handling
  to the one-day MVP.

## Consequences

- The key must be rotated if it leaks; it lives only in the owner's user profile.
- Switching to Entra later changes only the image client in Infrastructure.
