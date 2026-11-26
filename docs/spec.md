# Asset Forge – MVP spec and implementation plan

| | |
|---|---|
| **Status** | Plan for the project owner's approval. Implementation starts only after approval. |
| **Date** | 2026-11-26 |
| **Reference** | [requirements.md](requirements.md) v0.3 describes the full v1. This spec is the smaller MVP and wins where the two differ. Requirement IDs (FR-n, AC-n.m) point into requirements.md. |

## 1. Goal

A local ASP.NET Core Web API that turns a short request ("mossy stone door") plus a per-project style guide into 4 consistent image variants through an Azure Foundry image model, stores them, lets the owner approve or reject them, enforces a monthly budget cap, and exports approved images to a folder. It is demoable through the OpenAPI UI (Scalar), with tests and CI.

## 2. In scope

| # | Feature | Requirements used (thin where noted) |
|---|---|---|
| 1 | Create, list and read projects; each has one style guide | FR-1a, AC-1.1 |
| 2 | Read and save the style guide (FR-2 fields, no reference images, no version history; each request keeps its own copy, item 4) | FR-2, AC-2.1, AC-2.8 |
| 3 | Fixed preset text per asset type (prop, icon, tile, background); no sizes or post-processing | FR-4 (prompt text only), AC-4.1 |
| 4 | Create a request and expand it with a deterministic template (no text model). Each request stores a copy of the style guide as it was used, so images made before a later guide edit stay traceable. The prompt is returned and can be edited before generating. The original expansion, the final prompt and an "edited" flag are kept. | FR-5 fixed part, AC-5.1, AC-5.2, FR-10 (instead of FR-3 versions) |
| 5 | Generate 4 variants (or fewer, AC-11.4) through `IImageGenerator` (§4, image model). The 4 calls run in parallel and each variant succeeds or fails on its own. Paid images are never lost: closing the client or a timeout does not cancel model calls, and each variant is stored as soon as it returns. Content-filter refusals get their own error type. A retry runs only the failed variants and never the content-filtered ones. | FR-6, AC-6.1, AC-6.3, AC-6.4, AC-6.5 (thin), AC-6.7 |
| 6 | Store every variant, including failed and rejected ones, with its metadata (request, prompt, model, deployment, API version, seed, settings, cost, timestamps). PNGs go on disk. | FR-10 (thin), AC-10.2 |
| 7 | Monthly budget cap, default €100, checked before every round; cost recorded per call; warning at 80 %; calendar month in UTC; concurrent rounds cannot overspend | FR-11, AC-11.1 to AC-11.9 |
| 8 | Gallery: list images with filters (status, asset type, exported) and paging; approve, reject, mark on-style; view details and download the PNG | FR-9, AC-9.1 to AC-9.7, AC-10.1 |
| 9 | Export approved images under the configured export root, optionally into a named subfolder (§5). Safe file names and export records. The last subfolder used is remembered per project. | FR-13 (narrowed, §5), AC-13.1 to AC-13.3, AC-13.5 to AC-13.8 |
| 10 | Local-only access protection (§5) | FR-14, AC-14.1 to AC-14.3 |
| 11 | CI on GitHub Actions: build, test, coverage gate of 80 % | claude.md |

## 3. Out of scope for the MVP

- Multiple style guide versions (FR-3).
- Text-model expansion and feedback rounds (FR-5 subject part, FR-7).
- Automatic retries, generation that survives an app restart, and showing interrupted variants after a restart (the rest of FR-6). Open reservations are still settled at startup (§4).
- Transparency and background removal (FR-6, FR-12a), and all other post-processing (FR-12).
- Reference images (OQ-12).
- Provenance CSV (FR-15) and the operation log (FR-16).
- Bulk review actions and undo (FR-9 Should).
- A user interface other than the OpenAPI page, and hosting in Azure.

## 4. Architecture

- **ASP.NET Core Web API** on .NET 10 with minimal APIs, an OpenAPI document and the Scalar UI.
- **Layers** as separate projects in one solution:
  - `AssetForge.Api`: endpoints, access-protection middleware, ProblemDetails errors.
  - `AssetForge.Application`: services (projects, prompt template, generation, budget, review, export), entities, and the interfaces they need.
  - `AssetForge.Infrastructure`: EF Core DbContext and migrations, FLUX image client, file image store, export writer.
  - `AssetForge.Tests`: unit tests, plus endpoint tests through the in-memory test host with a fake image model.
- **External calls behind interfaces:** `IImageGenerator`, `IImageStore` and `IExportWriter`, with time from .NET's `TimeProvider`. Fakes are the default in tests, and no test calls Azure.
- **Data:** EF Core with SQLite in a local file. The database is migrated at startup.
- **Configuration:** appsettings plus environment variables. Secrets come from user-secrets locally (`Foundry:Endpoint`, `Foundry:ApiKey`, `Security:ApiToken`). Keys, tokens and full prompts are never logged.
- **Image model:** the MVP generates through `IImageGenerator` and does not depend on one model. The MVP model is chosen per DEC-9 after the spike; FLUX.2 [pro] is the spike model only. Until that decision, the MVP runs on the spike's FLUX.2 [pro] client: `POST {endpoint}/providers/blackforestlabs/v1/{deployment}?api-version=preview` with `Authorization: Bearer <key>`, and a body with the model, prompt, width, height, seed and `output_format: png`. The model is GA but this route only has `api-version=preview`. That is an accepted departure from DEC-3 (GA only) for the MVP, and it is reviewed when DEC-9 is decided. Model, deployment and API version come from configuration and are stored with each image.
- **Per-model limits are configuration, not code (NFR-9):** each model's settings hold its size limits (smallest side, largest total), maximum prompt length and prices. Requested sizes are checked against them at startup and before every call. For FLUX.2 [pro] (Appendix B) the output is at most 4 MP (1 MP = 1024 × 1024), the prompt at most 32,000 tokens, and no minimum side is documented. The MVP default output is 1024 × 1024 PNG.
- **Generation runs to completion:** model calls use the app's own lifetime, not the HTTP request's. Closing the client or hitting an HTTP timeout therefore does not cancel them. The 4 calls run in parallel. Each variant is stored as soon as it returns, with its PNG on disk, its row in the database and its spend settled, and it does not wait for the other three. If the client has gone, the results are still there in `GET /api/requests/{id}`. On startup, any reservation that is still open, for example after a crash, is settled at its estimate, so possibly billed calls stay counted (FR-11).
- **Budget concurrency:** a round first reserves its maximum estimated cost while holding a process-wide lock, so the check and the reservation happen together. As each call returns, it settles its own reservation: at the actual cost if the model reports one, otherwise at the estimate. A call that certainly was not billed releases its reservation. Reservations count as committed spend (FR-11).
- **Prices:** per-model configuration, so each model can have its own pricing rule. For FLUX.2 [pro] the rule is $0.03 for the first megapixel and $0.015 for each further megapixel, with output rounded up to whole megapixels (Appendix B). That makes 1024 × 1024 cost $0.03 and 1920 × 1080 cost $0.045. The USD→EUR rate is configured too. The price and rate used are stored with each call (FR-11, OQ-6).

## 5. Access protection (FR-14)

Every endpoint that spends money or touches files must be safe from other devices, from web pages open in the owner's browser, and from DNS rebinding. Origin checks alone are not enough. A rebound page counts as same-origin, and requests with no body or a simple body skip the CORS preflight. So the API uses these layers, all applied before any endpoint, model call or data access:

1. **Loopback only.** Kestrel listens on `localhost` only. Requests whose remote address is not loopback are refused, so other devices cannot connect (AC-14.1, AC-14.2).
2. **Host allowlist.** Only `localhost` and `127.0.0.1` are accepted as hosts, through ASP.NET Core host filtering (`AllowedHosts` is `localhost;127.0.0.1`). Any other host is refused with 400. A DNS-rebinding page sends its own host name (for example `evil.example`), so it is refused here (AC-14.3). The app does not start if `AllowedHosts` is `*`.
3. **API token.** Every `/api` request must carry `Authorization: Bearer <token>`. The token is a random value of at least 32 characters that the owner sets in user-secrets (`Security:ApiToken`), and the app does not start without it. A web page cannot know it. It is also a non-simple header, so a browser must send a CORS preflight first. The API answers no CORS requests, so the preflight fails. The token is compared in constant time and never logged. In Scalar, the owner enters it once in the authentication panel.
4. **Cross-site refusal.** State-changing requests from another origin are refused. They are checked with `Origin`, which must be absent or the app's own origin, and with `Sec-Fetch-Site`, where `cross-site` and `same-site` are refused. The `same-site` case covers another local app on a different port. This is defence in depth on top of 2 and 3.
5. **JSON bodies only.** Every POST and PUT needs a JSON body with `Content-Type: application/json`, even where the body is just `{}`, for example on approve. A missing body or any other content type is refused with 415. Browsers cannot send such a request cross-origin without a CORS preflight, and the preflight fails.
6. **Unguessable IDs.** Projects, requests, images and exports use GUIDs (`Guid.CreateVersion7()`), not sequential integers.
7. **Export root.** The request never takes a path. The export root comes only from configuration (`Export:Root`, for example the Unity project's `Assets/Art`), and it has no default: until the owner sets it, export is refused. The request may name a subfolder matching `^[a-z0-9-]{1,64}$`. The server builds the full path, refuses it unless it is still under the root, and refuses a subfolder or an existing target file that is a symlink or junction. File names are built on the server from slugs and the image ID (FR-13). This deliberately narrows FR-13 ("any folder the user can write to"). Only the owner can change the root, by editing configuration.

Tests cover each layer and show that nothing happens when a request is refused: no model call, no change in spend, and no file written. The refused cases are:
- a request from a non-loopback address;
- a wrong Host;
- a missing or wrong token;
- a cross-site `Origin` or `Sec-Fetch-Site`;
- a POST or PUT with no body, or with a form-encoded or `text/plain` body;
- an export subfolder that escapes the root (`..`, an absolute path, a symlink).

## 6. Endpoints

All paths are under `/api` and need the token from §5. Every POST and PUT sends a JSON body, `{}` where nothing else is needed. The OpenAPI document (`/openapi/v1.json`) and Scalar (`/scalar/v1`) contain no data and are served without the token.

| Method and path | Purpose |
|---|---|
| `POST /api/projects`, `GET /api/projects`, `GET /api/projects/{id}` | Create, list and read projects |
| `GET /api/projects/{id}/style-guide`, `PUT /api/projects/{id}/style-guide` | Read and save the style guide |
| `POST /api/projects/{id}/requests` | Create a request; returns the expanded prompt |
| `GET /api/requests/{id}` | The request with its style guide copy, and all its rounds and variants, including failures |
| `POST /api/requests/{id}/generate` | Body `{ prompt?, variants? }`. Generates a round and returns each variant with its outcome or error, plus the budget status. If the client disconnects, the round still completes and is stored |
| `POST /api/requests/{id}/retry` | Regenerates only the failed variants of the latest round (AC-6.3) |
| `GET /api/projects/{id}/images?status=&assetType=&exported=&page=&pageSize=` | Gallery of successfully generated images |
| `GET /api/images/{id}`, `GET /api/images/{id}/file` | Image details (FR-10) and the PNG |
| `POST /api/images/{id}/approve`, `POST /api/images/{id}/reject`, `PUT /api/images/{id}/on-style` | Review (FR-9). Rejecting an exported image returns a warning that the file stays (AC-9.6) |
| `GET /api/budget`, `PUT /api/budget` | The month's spend, remaining amount and warning; change the cap |
| `POST /api/projects/{id}/export` | Body `{ subfolder?, imageIds? }`. Exports approved images under `Export:Root` and reports each failure |

## 7. Delivery order (one GitHub issue each)

0. #12 Phase 0 spike: done locally; its notes feed DEC-9 and the model choice (§4).
1. #4 Solution skeleton, access protection (§5: hosts, token, cross-site refusal, JSON bodies), ProblemDetails, Scalar, CI with coverage gate. The protection goes in here because it is cheap in the skeleton and awkward to add later.
2. #5 Projects and style guide.
3. #6 Presets, prompt template and requests, with the style guide copy on each request.
4. #7 Model-neutral `IImageGenerator`, the fake for tests, and the FLUX.2 [pro] client as the interim implementation (§4).
5. #8 Budget: cap, reservation, recording and warning.
6. #9 Generation of 4 parallel variants, each stored as soon as it returns, not cancelled by the client; open reservations settled at startup.
7. #10 Review gallery.
8. #11 Export.

Each step comes as its own small pull request with tests, after this plan is approved.

## 8. Decisions

Confirmed by the project owner on 2026-11-26:

- SQLite is the local database.
- The target framework is .NET 10 (LTS).
- For the MVP, Foundry auth is an API key from user-secrets. Entra ID sign-in comes later (NFR-5).
- The MVP is model-neutral. The model is chosen per DEC-9 after the spike, and until then it runs on FLUX.2 [pro] with `api-version=preview`, which is an accepted departure from DEC-3.
- A copy of the style guide is stored with each request, instead of style guide versions (FR-3).
- Generation must not lose paid images (§4).
- Each NuGet package is reviewed with the code that adds it. The packages planned are below; all are MIT unless noted.
  - Microsoft.EntityFrameworkCore.Sqlite and Microsoft.EntityFrameworkCore.Design
  - Microsoft.AspNetCore.OpenApi and Scalar.AspNetCore
  - xunit and xunit.runner.visualstudio (both Apache-2.0)
  - Microsoft.NET.Test.Sdk, Microsoft.AspNetCore.Mvc.Testing and coverlet.msbuild
- Cross-site and DNS-rebinding protection is in scope (§5), because the endpoints spend money and write files.
- This file is `docs/spec.md`, the spec that claude.md names as the source of truth.
