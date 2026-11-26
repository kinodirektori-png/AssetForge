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
| 2 | Read and save the style guide (FR-2 fields, no reference images, no versioning) | FR-2, AC-2.1, AC-2.8 |
| 3 | Fixed preset text per asset type (prop, icon, tile, background); no sizes or post-processing | FR-4 (prompt text only), AC-4.1 |
| 4 | Create a request and expand it with a deterministic template (no text model). The prompt is returned and can be edited before generating. The original expansion, the final prompt and an "edited" flag are kept. | FR-5 fixed part, AC-5.1, AC-5.2 |
| 5 | Generate 4 variants (or fewer, AC-11.4) with FLUX.2 [pro] through the call proven in the spike. Each variant succeeds or fails on its own. Content-filter refusals get their own error type. A retry runs only the failed variants and never the content-filtered ones. | FR-6, AC-6.1, AC-6.3, AC-6.4, AC-6.7 |
| 6 | Store every variant, including failed and rejected ones, with its metadata (request, prompt, model, deployment, API version, seed, settings, cost, timestamps). PNGs go on disk. | FR-10 (thin), AC-10.2 |
| 7 | Monthly budget cap, default €100, checked before every round; cost recorded per call; warning at 80 %; calendar month in UTC; concurrent rounds cannot overspend | FR-11, AC-11.1 to AC-11.9 |
| 8 | Gallery: list images with filters (status, asset type, exported) and paging; approve, reject, mark on-style; view details and download the PNG | FR-9, AC-9.1 to AC-9.7, AC-10.1 |
| 9 | Export approved images under the configured export root, optionally into a named subfolder (§5). Safe file names and export records. The last subfolder used is remembered per project. | FR-13 (narrowed, §5), AC-13.1 to AC-13.3, AC-13.5 to AC-13.8 |
| 10 | Local-only access protection (§5) | FR-14, AC-14.1 to AC-14.3 |
| 11 | CI on GitHub Actions: build, test, coverage gate of 80 % | claude.md |

## 3. Out of scope for the MVP

- Multiple style guide versions (FR-3).
- Text-model expansion and feedback rounds (FR-5 subject part, FR-7).
- Automatic retries, background generation and crash recovery (the rest of FR-6).
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
- **Configuration:** appsettings plus environment variables. Secrets come from user-secrets locally (`Foundry:Endpoint`, `Foundry:ApiKey`, `Security:ApiToken`), and from Key Vault if this is ever hosted in Azure. Keys, tokens and full prompts are never logged.
- **Image model:** FLUX.2 [pro] through the documented Black Forest Labs route, as proven in the spike: `POST {endpoint}/providers/blackforestlabs/v1/{deployment}?api-version=preview` with `Authorization: Bearer <key>`. The body has the model, prompt, width, height, seed and `output_format: png`. Output is 1024 × 1024 by default. Configured sizes must keep each side ≥ 768 and the total ≤ 2,359,296 pixels.
- **Budget concurrency:** a round first reserves its maximum estimated cost while holding a process-wide lock, so the check and the reservation happen together. Each call then settles its reservation: at the actual cost if the model reports one, otherwise at the estimate. A call that certainly was not billed releases its reservation. Reservations count as committed spend (FR-11).
- **Prices:** the price per megapixel (USD) and the USD→EUR rate are configured values, stored with each call (FR-11, OQ-6).

## 5. Access protection (FR-14)

Every endpoint that spends money or touches files must be safe from other devices, from web pages open in the owner's browser, and from DNS rebinding. Origin checks alone are not enough. A rebound page counts as same-origin, and requests with no body or a simple body skip the CORS preflight. So the API uses these layers, all applied before any endpoint, model call or data access:

1. **Loopback only.** Kestrel listens on `localhost` only. Requests whose remote address is not loopback are refused, so other devices cannot connect (AC-14.1, AC-14.2).
2. **Host allowlist.** ASP.NET Core host filtering with `AllowedHosts` set to `localhost;127.0.0.1;[::1]`. Any other host is refused with 400. A DNS-rebinding page sends its own host name (for example `evil.example`), so it is refused here (AC-14.3). The app does not start if `AllowedHosts` is `*`.
3. **API token.** Every `/api` request must carry `Authorization: Bearer <token>`. The token is a random value of at least 32 characters that the owner sets in user-secrets (`Security:ApiToken`), and the app does not start without it. A web page cannot know it. It is also a non-simple header, so a browser must send a CORS preflight first. The API answers no CORS requests, so the preflight fails. The token is compared in constant time and never logged. In Scalar, the owner enters it once in the authentication panel.
4. **Cross-site refusal.** Requests are refused when their `Origin` is present and is not the app's own origin, or when `Sec-Fetch-Site` is `cross-site` or `same-site`. The `same-site` case covers another local app on a different port. This is defence in depth on top of 2 and 3.
5. **JSON only.** Request bodies must be `application/json`. Form posts and `text/plain` bodies are refused with 415.
6. **Unguessable IDs.** Projects, requests, images and exports use GUIDs (`Guid.CreateVersion7()`), not sequential integers.
7. **Export root.** The request never takes a path. The export root comes only from configuration (`Export:Root`, for example the Unity project's `Assets/Art`), and it has no default: until the owner sets it, export is refused. The request may name a subfolder matching `^[a-z0-9-]{1,64}$`. The server builds the full path, refuses it unless it is still under the root, and refuses a subfolder or an existing target file that is a symlink or junction. File names are built on the server from slugs and the image ID (FR-13). This deliberately narrows FR-13 ("any folder the user can write to"). Only the owner can change the root, by editing configuration.

Tests cover each layer and show that nothing happens when a request is refused: no model call, no change in spend, and no file written. The refused cases are:
- a request from a non-loopback address;
- a wrong Host;
- a missing or wrong token;
- a cross-site `Origin` or `Sec-Fetch-Site`;
- a form-encoded or `text/plain` body;
- an export subfolder that escapes the root (`..`, an absolute path, a symlink).

## 6. Endpoints

All paths are under `/api` and need the token from §5. The OpenAPI document (`/openapi/v1.json`) and Scalar (`/scalar/v1`) contain no data and are served without the token.

| Method and path | Purpose |
|---|---|
| `POST /api/projects`, `GET /api/projects`, `GET /api/projects/{id}` | Create, list and read projects |
| `GET /api/projects/{id}/style-guide`, `PUT /api/projects/{id}/style-guide` | Read and save the style guide |
| `POST /api/projects/{id}/requests` | Create a request; returns the expanded prompt |
| `GET /api/requests/{id}` | The request with all its rounds and variants, including failures |
| `POST /api/requests/{id}/generate` | Optional body `{ prompt, variants }`. Generates a round; returns each variant with its outcome or error, and the budget status |
| `POST /api/requests/{id}/retry` | Regenerates only the failed variants of the latest round (AC-6.3) |
| `GET /api/projects/{id}/images?status=&assetType=&exported=&page=&pageSize=` | Gallery of successfully generated images |
| `GET /api/images/{id}`, `GET /api/images/{id}/file` | Image details (FR-10) and the PNG |
| `POST /api/images/{id}/approve`, `POST /api/images/{id}/reject`, `PUT /api/images/{id}/on-style` | Review (FR-9). Rejecting an exported image returns a warning that the file stays (AC-9.6) |
| `GET /api/budget`, `PUT /api/budget` | The month's spend, remaining amount and warning; change the cap |
| `POST /api/projects/{id}/export` | Optional body `{ subfolder, imageIds }`. Exports approved images under `Export:Root` and reports each failure |

## 7. Delivery order (one GitHub issue each)

1. #4 Solution skeleton, access protection (§5), ProblemDetails, Scalar, CI with coverage gate.
2. #5 Projects and style guide.
3. #6 Presets, prompt template and requests.
4. #7 Image generator interface, FLUX client, and the fake for tests.
5. #8 Budget: cap, reservation, recording and warning.
6. #9 Generation of 4 variants with storage and metadata.
7. #10 Review gallery.
8. #11 Export.

Each step comes as its own small pull request with tests, after this plan is approved.

## 8. Decisions

Confirmed by the project owner on 2026-11-26:

- SQLite is the local database.
- The target framework is .NET 10 (LTS).
- For the MVP, Foundry auth is an API key from user-secrets. Entra ID sign-in comes later (NFR-5).
- Each NuGet package is reviewed with the code that adds it. The packages planned are below; all are MIT unless noted.
  - Microsoft.EntityFrameworkCore.Sqlite and Microsoft.EntityFrameworkCore.Design
  - Microsoft.AspNetCore.OpenApi and Scalar.AspNetCore
  - xunit and xunit.runner.visualstudio (both Apache-2.0)
  - Microsoft.NET.Test.Sdk, Microsoft.AspNetCore.Mvc.Testing and coverlet.collector
- Cross-site and DNS-rebinding protection is in scope (§5), because the endpoints spend money and write files.
- This file is `docs/spec.md`, the spec that claude.md names as the source of truth.
