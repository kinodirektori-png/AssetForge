# 0008. Generation runs to completion and stores each variant at once

- Status: Accepted
- Date: 2026-11-26
- Source: Owner decision (plan review)
- Related: spec §4; FR-6, FR-11; issue #9; 0009

## Context

Each variant is a paid model call. If the client disconnects or the HTTP request times out, cancelling the calls or
dropping their results would lose images that were already billed.

## Decision

- Model calls run on the app's lifetime, not the HTTP request's. Closing the client or a timeout does not cancel them.
- The 4 calls run in parallel. Each variant is stored as soon as it returns (PNG on disk, database row, spend settled)
  without waiting for the others.
- Results stay readable in `GET /api/requests/{id}` even if the client has gone.
- On startup, any open budget reservation (for example after a crash) is settled at its estimate.

## Options considered

- **Store all 4 at the end of the round.** Simpler, but one failure or a disconnect loses paid images.
- **Survive an app restart mid-round.** Needs a job queue; out of scope for the MVP (spec §3).

## Consequences

- Spend may be counted at the estimate for calls that were in flight during a crash. That errs on the side of the cap.
- Tests must cover a disconnect mid-round and a restart with open reservations.
