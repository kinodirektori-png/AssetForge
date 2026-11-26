# 0004. Layered local-only access protection

- Status: Accepted
- Date: 2026-11-26
- Source: Owner decision (plan review put cross-site protection back in scope); deny-by-default token, AllowedHosts check
  and security headers from review of #4
- Related: spec §5; FR-14, AC-14.1 to AC-14.3; DEC-1; 0011

## Context

The API runs on the owner's computer but spends money (model calls) and writes files (export). Listening on localhost
is not enough: any web page open in the owner's browser can send requests to localhost, requests with no body or a
simple body skip the CORS preflight, SQLite integer ids are easy to guess, and DNS rebinding defeats Origin checks.
The first plan dropped this protection; the owner caught it.

## Decision

Every layer runs before any endpoint, model call or data access:

1. Kestrel listens on localhost only; non-loopback remote addresses get 403.
2. `AllowedHosts` is `localhost;127.0.0.1`; any other Host gets 400. The app refuses to start if it lists anything else.
3. A Bearer token (`Security:ApiToken`, at least 32 characters, from user-secrets) is required on every path except
   `/`, `/openapi/*` and `/scalar/*` (deny by default). Compared in constant time, never logged. No CORS.
4. A cross-origin `Origin`, or `Sec-Fetch-Site` of `cross-site` or `same-site`, gets 403.
5. Every POST, PUT and PATCH needs a non-empty `application/json` body (`{}` where nothing else is needed); otherwise 415.
6. Ids are GUID v7, not sequential integers.
7. Security headers on every response, including 500s: a CSP (`default-src 'none'` for the API), `nosniff`,
   `no-referrer`.
8. Export writes only under a configured root (0011).

Tests cover each layer on its own, and every feature issue tests that a refused request makes no model call, changes no
spend and writes no file.

## Options considered

- **Origin check only.** Defeated by DNS rebinding and by requests that send no Origin.
- **Token only.** Strong on its own, but one mistake in an allowlist would expose everything; the other layers are cheap.
- **Allowlist of protected paths instead of deny by default.** A new endpoint could be left open by mistake.

## Consequences

- Scalar needs the token entered once in its auth panel.
- Every POST in tests and clients must send a JSON body.
- A test enumerates every mapped endpoint and checks it returns 401 without the token, so new endpoints are covered
  automatically.
