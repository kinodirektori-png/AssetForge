# 0011. Export only under a configured root

- Status: Accepted
- Date: 2026-11-26
- Source: Owner decision (plan review raised that export wrote to any folder named in the request)
- Related: spec §5 item 7; FR-13 (narrowed), DEC-7; issue #11; 0004

## Context

FR-13 lets the user export to any folder they can write to. Through an API, that means a request can name any path,
so a mistake or a forged request could write files anywhere the user can.

## Decision

- The request never takes a path. The export root comes only from configuration (`Export:Root`) and has no default;
  until it is set, export is refused.
- A request may name a subfolder matching `^[a-z0-9-]{1,64}$`. The server builds the full path, resolves it, and refuses
  it unless it is still under the root. A subfolder or target file that is a symlink or junction is refused.
- File names are built on the server from slugs and the image id.

## Options considered

- **Any path in the request, validated.** Path validation across Windows and Linux is easy to get wrong.

## Consequences

- This deliberately narrows FR-13. Only the owner can change the root, by editing configuration.
