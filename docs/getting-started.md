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
`{}` where nothing else is needed. The SQLite database is kept in `src/AssetForge.Api/data/`.

## Tests

```
dotnet test AssetForge.slnx --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults
python3 scripts/check-coverage.py 80
```
