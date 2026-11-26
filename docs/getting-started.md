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

It listens on http://localhost:5062 only and opens the Scalar UI at `/scalar/v1`. In Scalar, open the authentication
setting and paste the token once. `GET /api/status` checks that everything works. Every POST and PUT needs a JSON body,
`{}` where nothing else is needed.

## Where data is kept

The SQLite database and, later, the generated images live outside the repository, in `%LOCALAPPDATA%\AssetForge`
on Windows (`~/.local/share/AssetForge` on Linux). To use another folder, set `Storage:DataRoot`; to point at another
database only, set `ConnectionStrings:AssetForge`. Either can go in user-secrets or an environment variable
(`Storage__DataRoot`).

## Tests

```
dotnet test AssetForge.slnx --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults
```

Then check the 80 % line-coverage gate. The check reads the newest report in `TestResults`. On Windows:

```
pwsh scripts/check-coverage.ps1 80
```

With Python instead: `python3 scripts/check-coverage.py 80`. CI runs both, on Windows and Linux.
