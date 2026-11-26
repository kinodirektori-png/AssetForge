# 0001. .NET 10 Web API with minimal APIs in three layers

- Status: Accepted
- Date: 2026-11-26
- Source: Owner decision (stack and layers); minimal APIs approved with the spec
- Related: spec §4, §8; DEC-1

## Context

Asset Forge is a single-user local app (DEC-1) driven through an HTTP API. The owner works in .NET and asked for an
ASP.NET Core Web API with EF Core, split into API endpoints, application services with interfaces, and infrastructure
implementations.

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
- **One project.** Simpler, but the owner asked for the layers, and they keep model and file access swappable.

## Consequences

- Application references the ASP.NET Core shared framework for a few types. Accepted in review of #4 as pragmatic; it
  can be narrowed later.
- Each feature adds an endpoint file in Api, a service in Application and, where needed, an implementation in
  Infrastructure.
