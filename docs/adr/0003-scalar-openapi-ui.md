# 0003. Scalar as the OpenAPI UI, served locally

- Status: Accepted
- Date: 2026-11-26
- Source: Approved with the spec. The original brief said "demoable via Swagger/Scalar"; Scalar was picked when the spec
  was written.
- Related: spec §1, §4, §5; 0004

## Context

The MVP has no user interface of its own; it is demonstrated through an OpenAPI page. Since .NET 9, ASP.NET Core
generates the OpenAPI document itself (`Microsoft.AspNetCore.OpenApi`), and the templates no longer include Swashbuckle
or Swagger UI. The page must work with the Bearer token (0004) and must not let a third-party script read that token.

## Decision

- Use **Scalar** (`Scalar.AspNetCore`, MIT) on top of the built-in OpenAPI document, at `/scalar/v1`; `/` redirects there.
- Scalar's scripts are bundled in the package and served by the app. Its default fonts (from fonts.scalar.com) are
  turned off, so it uses system fonts.
- A Content-Security-Policy on the Scalar pages allows scripts, styles, fonts and requests only from the app itself.

## Options considered

- **Swagger UI** (`Swashbuckle.AspNetCore.SwaggerUI`, MIT) on the same document. Familiar and works fine; it is a
  separate package that .NET no longer ships with. A small swap if the owner prefers it.
- **No UI, only the document** with an external client. Fails the "demoable" goal.

## Consequences

- Scalar's cloud features (Ask AI, Generate SDK, Deploy) are blocked by the CSP and do nothing. They can be hidden.
- A Scalar upgrade could add new external assets. The CSP blocks them, and the security-header tests catch a changed
  policy, but a feature might silently stop working; check the browser console after an upgrade.
