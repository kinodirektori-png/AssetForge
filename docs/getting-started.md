# Getting started

Asset Forge is a local ASP.NET Core Web API. See [spec.md](spec.md) for what it does.

## Requirements

- .NET 10 SDK

## One-time setup

From `src/AssetForge.Api`, set the API token (spec §5). It must be a random value of at least 32 characters:

```
dotnet user-secrets set "Security:ApiToken" "<random value>"
```

In PowerShell, `[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))` gives a good value.
The app does not start without it.

## Run

```
dotnet run --project src/AssetForge.Api
```

It listens on http://localhost:5062 only (set `Server:Port` to use another port) and opens the Scalar UI at `/scalar/v1`. In Scalar, open the authentication
setting and paste the token once. `GET /api/status` checks that everything works. Every POST and PUT needs a JSON body,
`{}` where nothing else is needed, and any request body must be `application/json`.

## Where data is kept

The SQLite database and, later, the generated images live outside the repository, in `%LOCALAPPDATA%\AssetForge`
on Windows (`~/.local/share/AssetForge` on Linux). To use another folder, set `Storage:DataRoot` to an absolute path (a relative one is taken as a subfolder of that default, never of the folder you start the app from); to point at another
database only, set `ConnectionStrings:AssetForge`. Either can go in user-secrets or an environment variable
(`Storage__DataRoot`).

## Database changes

The database is migrated automatically when the app starts. After changing the EF Core model, add a migration with the
`dotnet-ef` tool. Its version is pinned in `.config/dotnet-tools.json` to match the EF Core packages, so restore it once
instead of installing it globally:

```
dotnet tool restore
dotnet ef migrations add <Name> --project src/AssetForge.Infrastructure --output-dir Data/Migrations
```

Timestamps are stored as UTC text in ISO 8601 form (`DateTimeOffsetToUtcIsoStringConverter`), so the database can sort
and filter by them. The converter is not recorded in migrations, so a change to it needs no migration but does change how
new values are stored. Reading is lenient: a timestamp written in an earlier form (for example with a local offset) is
still read correctly, so such a change does not require deleting the database. Values written before the change are
not rewritten, so they may sort out of order against new ones.

## Tests

```
dotnet test AssetForge.slnx
```

To check coverage the way the CI "Coverage (80 % lines)" check does:

```
dotnet test AssetForge.slnx -p:CollectCoverage=true
```

It prints line, branch and method coverage per project and fails if total line coverage is under 80 %. The report is
written to `TestResults/coverage.cobertura.xml`. A pull request can only be merged into `Main` when both the build and
the coverage check pass.
