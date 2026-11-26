# 0010. A style guide copy on each request instead of versions

- Status: Accepted
- Date: 2026-11-26
- Source: Owner decision (plan review)
- Related: spec §2 item 4; FR-3, FR-10; issue #6

## Context

Style guide versioning (FR-3) is out of scope for the MVP. Without it, editing the guide would make older images
untraceable: nobody could tell which guide produced them.

## Decision

Each request stores a copy of the style guide as it was used.

## Options considered

- **Full versioning (FR-3).** More work than the MVP needs.
- **No copy.** Loses traceability after the first edit.

## Consequences

- Some duplicated data per request; small compared with the images.
- If FR-3 comes later, the copies can be migrated into versions.
