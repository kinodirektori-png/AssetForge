# 0002. SQLite through EF Core, data in the user profile

- Status: Accepted
- Date: 2026-11-26
- Related: spec §4, §8; NFR-6, NFR-7

## Context

The app stores projects, style guides, requests, image metadata, budget records and exports for one user on one
computer. The PNG files go on disk.

## Decision

- **SQLite** through **EF Core**, migrated at startup.
- Data lives in `%LOCALAPPDATA%\AssetForge` (`~/.local/share/AssetForge` on Linux), not in the repository or next to the
  app. `Storage:DataRoot` changes the folder; a relative value is taken under the default folder, never the working
  directory. `ConnectionStrings:AssetForge` can point at another database.

## Options considered

- **SQL Server LocalDB or PostgreSQL.** Need an installed server; nothing the MVP needs justifies that.
- **Database under the API project folder.** Mixes user data with source, is lost on a fresh clone, and changes with
  the folder the app is started from.

## Consequences

- No server to install. Backups are a file copy.
- SQLite has limited concurrent writers; the single-user app is far below that limit. Budget concurrency is handled in
  the app (0009).
