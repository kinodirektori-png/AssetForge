# 0001. .NET 10 Web API with minimal APIs in three layers

- Status: Accepted
- Date: 2026-11-26
- Related: spec §4, §8; DEC-1

## Context

Asset Forge is a single-user local app (DEC-1) driven through an HTTP API. It calls a paid cloud model and writes files,
both of which must be replaceable by fakes in tests, and the model itself is expected to change after the spike (0006).
The code base is .NET, and the app has to run on Windows.

## Decision

- ASP.NET Core Web API on **.NET 10 (LTS)**, with **minimal APIs**.
- Three projects plus tests: `AssetForge.Api` (endpoints, middleware, errors), `AssetForge.Application` (services,
  entities, interfaces), `AssetForge.Infrastructure` (EF Core, image client, file store, export writer).
- External calls sit behind interfaces (`IImageGenerator`, `IImageStore`, `IExportWriter`) and time comes from
  `TimeProvider`, so tests use fakes and never call Azure.
- Errors are ProblemDetails. Configuration comes from appsettings and environment variables; secrets from user-secrets.
- Warnings are errors (`Directory.Build.props`).

## Options considered

- **Controllers instead of minimal APIs.** Both were allowed. Minimal APIs need less ceremony for about 20 endpoints.
- **.NET 8 or 9.** .NET 10 is the current LTS release.
- **One project.** Simpler, but nothing stops endpoints from calling the model or the database directly, which makes
  the model swap and fake-based tests harder.

## Consequences

- Application references the ASP.NET Core shared framework for a few types. This is a small leak of web
  types into the core, kept because the alternative is wrapper types for little gain; it can be narrowed later.
- Each feature adds an endpoint file in Api, a service in Application and, where needed, an implementation in
  Infrastructure.
