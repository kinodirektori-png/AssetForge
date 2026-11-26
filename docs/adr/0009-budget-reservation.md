# 0009. Budget reserved before a round, settled per call

- Status: Accepted
- Date: 2026-11-26
- Related: spec §4; FR-11, AC-11.5, DEC-2; issue #8; 0008

## Context

There is one global monthly cap, €100 by default (DEC-2). Two rounds started at the same time must not both pass the
check and overspend together.

## Decision

- A round first reserves its maximum estimated cost while holding a process-wide lock, so the check and the reservation
  happen together.
- Each call settles its own reservation: at the actual cost if the model reports one, otherwise at the estimate. A call
  that certainly was not billed releases its reservation. Failed calls that may be billed still count.
- Reservations count as committed spend. The month is a calendar month in UTC; a warning shows at 80 %.

## Options considered

- **Check the cap without reserving.** Concurrent rounds can both pass.
- **Database-level locking only.** SQLite locking is coarse; a process lock is simpler for a single-process app.

## Consequences

- The lock assumes one app instance, which DEC-1 (local, single user) guarantees.
