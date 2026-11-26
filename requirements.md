# Asset Forge – Requirements

| | |
|---|---|
| **Status** | Draft v0.1 for review |
| **Owner** | Hannes |
| **Date** | 2026-11-26 |
| **Scope of this document** | *What* the system must do and *how well*. *How* it is built (architecture, components, storage technology) is decided in a later step. |

Requirement keywords: **Must** = required for v1, **Should** = wanted for v1 but can slip, **Could** = nice-to-have.
Items marked **OQ-n** refer to [Open questions](#13-open-questions).

---

## 1. Problem statement

Hannes is building a Unity game that needs many 2D images: props, tiles, icons and backgrounds. There is no access to a good general image generator. Free tools give inconsistent results: each image comes out in a different style, perspective or palette, and time is wasted rewriting prompts by hand.

## 2. Product summary

Asset Forge is a small service that keeps a per-project style guide and uses it to turn short requests (for example "mossy stone door") into detailed, consistent image prompts. It generates variants through an Azure-hosted image model, stores them, and lets the user approve the ones to export into the Unity project.

## 3. Goals and non-goals

### 3.1 Goals

- **G1 Consistency.** Images generated for one project share the same style, perspective and palette without the user rewriting prompts.
- **G2 Speed.** Going from a short request to an approved asset in the Unity project is fast (target in [§11](#11-success-metrics)).
- **G3 Low effort.** The user types a short request; the system supplies the detail from the style guide and asset-type rules.
- **G4 Control.** The user decides which images are used; nothing reaches the Unity project without approval.
- **G5 Cost control.** Spending on image generation stays within a fixed monthly budget.
- **G6 Traceability.** For every image it is known how it was made (request, prompt, style guide version, model, settings, cost).

### 3.2 Non-goals (out of scope for v1)

- Animation frames and sprite sheets.
- 3D assets.
- Fine-tuning or training a style model.
- Multiple users, accounts or logins.
- Editing parts of an image inside Asset Forge (inpainting / region editing as a user feature).
- Building sprite atlases (left to Unity).
- Strict pixel-art output (see assumption A3).

## 4. Users and key scenarios

### 4.1 Users

| User | Description |
|---|---|
| Game developer (single user) | Hannes. Builds the Unity game, owns the art direction, approves every asset. Only user of v1. |

### 4.2 Key scenarios

- **S1 Set up a project.** The user creates a project for a game, fills in its style guide (description, perspective, palette, lighting, outlines, detail level, things to avoid, reference images) and saves it.
- **S2 Request an asset.** The user picks an asset type (for example *prop*), types "mossy stone door", reviews the expanded prompt, optionally edits it, and generates 4 variants.
- **S3 Review and approve.** The user looks at the variants in a gallery, approves the good ones and rejects the rest.
- **S4 Iterate.** None of the variants is right; the user regenerates with short feedback ("more moss, less glow") and gets new variants.
- **S5 Export.** The user exports approved images; they appear in the Unity project in the right folder, with consistent names and import settings for their asset type.
- **S6 Watch spending.** The user sees how much of this month's budget is used and is warned before it runs out.
- **S7 Evolve the style.** The user changes the style guide; older images remain traceable to the style guide version that produced them.

## 5. Domain terms

| Term | Meaning |
|---|---|
| Project | One game (or prototype). Owns one style guide, its requests, images and budget. |
| Style guide | Project-wide visual rules, versioned. |
| Asset type | One of: *prop*, *tile*, *icon*, *background*. Each has a preset of extra rules. |
| Request | A short description from the user plus an asset type. |
| Expanded prompt | The detailed prompt built from the request, the style guide and the asset-type preset. |
| Variant | One generated image for a request. |
| Status | *Pending review*, *Approved*, *Rejected*, *Exported*. |

## 6. Functional requirements

Each requirement has acceptance criteria in Given / When / Then form.

### FR-1 Projects — Must

The system must let the user create, view, rename and list projects. Each project has its own style guide, requests, images and budget.

- **AC-1.1** Given no projects exist, when the user creates a project named "Dungeon", then "Dungeon" appears in the project list with an empty style guide.
- **AC-1.2** Given two projects "Dungeon" and "Forest", when the user opens "Forest", then only Forest's style guide, requests and images are shown.
- **AC-1.3** Given a project "Dungeon", when the user renames it to "Crypt", then the project list shows "Crypt" and its style guide, images and budget are unchanged.

### FR-2 Style guide — Must

Each project must have a style guide with these fields:

| Field | Content |
|---|---|
| Style description | Short free text, e.g. "stylised hand-painted, soft shading". |
| Camera / perspective | e.g. "¾ top-down". |
| Palette | 5 to 8 colours as hex values. |
| Lighting direction | e.g. "light from top-left". |
| Outline weight | e.g. "thin dark outline". |
| Detail level | e.g. "medium". |
| Things to avoid | List of terms that must not appear (e.g. "photorealism, text, watermark"). |
| Reference images | 1 to 3 images that show the target style. |

- **AC-2.1** Given a project, when the user enters all style guide fields and saves, then reopening the project shows the same values.
- **AC-2.2** Given the user enters 4 or 9 palette colours, when they save, then the save is refused with a message that 5 to 8 colours are required.
- **AC-2.3** Given the user enters a palette value that is not a valid hex colour, when they save, then the save is refused and the invalid value is pointed out.
- **AC-2.4** Given a style guide with 3 reference images, when the user tries to add a fourth, then it is refused with a message that the maximum is 3.

### FR-3 Style guide versioning — Must

Every saved change to a style guide must create a new version. Earlier versions remain viewable.

- **AC-3.1** Given a style guide at version 1, when the user changes the palette and saves, then the style guide is at version 2 and version 1 is still viewable with its original palette.
- **AC-3.2** Given images generated under version 1, when the style guide moves to version 2, then those images still show "style guide v1" in their details.

### FR-4 Asset-type presets — Must

Each asset type must have a preset of rules that is applied on top of the project style guide:

| Asset type | Preset rules |
|---|---|
| Prop | One object, centred, transparent background, 512 px. |
| Tile | Seamless, same perspective as the project, 256 px or 512 px. |
| Icon | 256 px, strong silhouette, transparent background. |
| Background | 16:9, no characters. |

Whether presets can be edited per project is open (OQ-10). The exact background resolution is open (OQ-14).

- **AC-4.1** Given asset type *icon*, when the system builds the expanded prompt, then the prompt includes the icon rules (strong silhouette, transparent background) as well as the project style guide.
- **AC-4.2** Given asset type *background*, when the system builds the expanded prompt, then the prompt includes "no characters" and a 16:9 format.
- **AC-4.3** Given asset type *tile*, when the user creates a request, then the user can choose 256 px or 512 px.

### FR-5 Request and prompt expansion — Must

The user must be able to submit a short request (free text plus asset type). The system must expand it into a detailed prompt that combines the request, the current style guide and the asset-type preset. The expanded prompt must be shown to the user before generation and must be editable. The final prompt actually sent must be saved with every image.

- **AC-5.1** Given a project with a style guide and the request "mossy stone door" of type *prop*, when the user asks for expansion, then an expanded prompt is shown that contains the request, the style guide's perspective, palette, lighting, outline and detail rules, its things-to-avoid list, and the prop preset rules.
- **AC-5.2** Given an expanded prompt is shown, when the user edits it and generates, then the edited text (not the original expansion) is what is sent to the image model and saved with each variant.
- **AC-5.3** Given an expanded prompt is shown, when the user generates without editing, then the expansion is sent unchanged and saved with each variant.

### FR-6 Variant generation — Must

For each request the system must generate 4 variants using the Azure-hosted image model. Props and icons must be delivered with transparent backgrounds.

- **AC-6.1** Given an approved prompt and enough budget, when the user generates, then 4 variants appear for review with status *Pending review*.
- **AC-6.2** Given a request of type *prop* or *icon*, when variants are generated, then each delivered PNG has a transparent background.
- **AC-6.3** Given the image model returns an error for one or more variants, when generation finishes, then the user sees which variants failed and why, the successful ones are kept, and the request can be retried.
- **AC-6.4** Given the budget is exhausted (see FR-11), when the user tries to generate, then no call is made to the image model and the user is told the budget is reached.

### FR-7 Iteration with feedback — Should

The user should be able to regenerate a request with short feedback text. New variants are linked to the original request.

- **AC-7.1** Given a request with 4 variants, when the user enters "more moss, less glow" and regenerates, then 4 new variants are produced whose saved prompt includes the feedback, and they are listed under the same request.
- **AC-7.2** Given several regeneration rounds on one request, when the user views the request, then every round's variants and feedback are visible in order.

### FR-8 Variations of an approved image — Could

The user could create new variants based on an already approved image (keeping its look while changing details).

- **AC-8.1** Given an approved image, when the user asks for variations with the text "add a lantern", then new variants are generated that use the approved image as a reference, and are linked to it.

### FR-9 Review gallery — Must

The system must show the images of a project in a gallery where the user can approve or reject each image, and regenerate a request.

- **AC-9.1** Given a project with images in several statuses, when the user opens the gallery, then each image shows its thumbnail, asset type, request text and status.
- **AC-9.2** Given a variant with status *Pending review*, when the user approves it, then its status becomes *Approved*.
- **AC-9.3** Given a variant with status *Pending review*, when the user rejects it, then its status becomes *Rejected*, and it is still stored (see FR-10).
- **AC-9.4** Given the gallery, when the user filters by status or asset type, then only matching images are shown.

### FR-10 Storage and traceability — Must

Every generated image must be stored, including rejected ones, together with:
the request text, asset type, feedback (if any), final prompt, style guide version, model name and version, generation settings, cost, status and timestamps.

- **AC-10.1** Given any stored image, when the user opens its details, then all fields listed above are shown.
- **AC-10.2** Given a rejected image, when the user opens the gallery filtered to *Rejected*, then the image and its details are still available.
- **AC-10.3** Given an image exported last month, when the user looks it up, then they can see exactly which prompt, style guide version and model produced it.

Retention and storage limits are open (OQ-11).

### FR-11 Monthly budget — Must

Each project must have a monthly generation budget, default **€100**. The system must record the cost of each generation, show current spend against budget, warn at 80 % and block new generations at 100 %.

- **AC-11.1** Given a new project, when the user opens its settings, then the monthly budget is €100 and can be changed.
- **AC-11.2** Given generations, when each one completes, then its cost is recorded and the month's spend increases by that amount.
- **AC-11.3** Given spend reaches 80 % of the budget, when the user next views the project or generates, then a warning shows the amount spent and remaining.
- **AC-11.4** Given a generation would take spend above 100 %, when the user tries to generate, then the generation is blocked (see AC-6.4).
- **AC-11.5** Given a new calendar month starts, when the user views the budget, then spend for the new month starts at €0.

How cost is determined, the currency conversion, and whether the budget is per project or overall are open (OQ-6, OQ-7).

### FR-12 Post-processing — Must / Could

- **FR-12a (Must)** Delivered images must be resized to the asset type's target size and transparent edges must be trimmed for props and icons.
- **FR-12b (Could)** Tiles could be checked for visible seams, and the user warned when a seam is detected.

- **AC-12.1** Given the model returns a 1024 × 1024 image for an *icon*, when the image is delivered, then it is 256 × 256 px.
- **AC-12.2** Given a *prop* with empty transparent margins, when the image is delivered, then the transparent margin is trimmed and the object stays centred in the 512 px canvas. *(Exact trim rule open, OQ-18.)*
- **AC-12.3 (Could)** Given a tile whose left and right edges do not match, when it is delivered, then the user sees a seam warning on that tile.

### FR-13 Export to Unity — Must

The user must be able to export approved images into the Unity project:

- PNG files placed under a configurable folder in the Unity project, by type, default `Assets/Art/<type>/`.
- Consistent file names (convention open, OQ-9).
- Unity import settings for the asset type applied: sprite mode, pixels per unit, filter mode (values open, OQ-9).

- **AC-13.1** Given 3 approved props, when the user exports, then 3 PNG files appear in `Assets/Art/prop/` of the Unity project and their status becomes *Exported*.
- **AC-13.2** Given exported icons, when Unity imports them, then they are imported as sprites with the configured pixels per unit and filter mode for icons, without manual changes.
- **AC-13.3** Given a rejected or pending image, when the user exports, then it is not exported.
- **AC-13.4** Given the user changed the export folder setting, when they export, then files go to the new folder.

How the export reaches the local Unity project is open (OQ-3).

## 7. Non-functional requirements

| ID | Area | Requirement | Target / measure |
|---|---|---|---|
| NFR-1 | Consistency | Variants follow the style guide without manual prompt editing. | ≥ 3 of 4 variants judged on-style by the user (see SM-1). |
| NFR-2 | Efficiency | Short time from request to asset in Unity. | < 5 minutes end to end (see SM-2). Generation latency per request: open (OQ-15). |
| NFR-3 | Cost | Spending is capped. | ≤ €100 per month per project (FR-11); no generation call is made once the budget is reached. |
| NFR-4 | Traceability | Every image can be traced to how it was made. | 100 % of stored images have all FR-10 fields. |
| NFR-5 | Security | Azure credentials are never exposed publicly. | Credentials do not appear in the repository, the published site, or responses sent to the browser. |
| NFR-6 | Access | Only the owner can use the service and spend the budget. | Mechanism open, given "no logins" plus public hosting (OQ-2). |
| NFR-7 | Data safety | Stored images and history are not lost. | No loss of stored images or metadata across restarts/redeploys. Retention limits open (OQ-11). |
| NFR-8 | Resilience | Model errors don't lose work. | A failed generation leaves the request and prompt intact and retryable (AC-6.3). |
| NFR-9 | Portability of model | The chosen image model can be replaced later without changing the user workflow. | Changing the model requires no change to FR-1 to FR-13 behaviour. |
| NFR-10 | Volume | Handles the expected load. | Tens to a few hundred images per week (assumption A5). |

## 8. Constraints

| ID | Constraint | Source |
|---|---|---|
| C-1 | Codebase in **.NET**. | Hannes, 2026-11-26 |
| C-2 | Hosted on **GitHub Pages**. | Hannes, 2026-11-26 |
| C-3 | Images generated by an **Azure-hosted image model**; gpt-image is not in the available Foundry model list, so **MAI-Image** models are the candidates (see [Appendix A](#appendix-a-mai-image-model-evaluation)). | Hannes, 2026-11-26 |
| C-4 | Monthly generation budget **€100**. | Hannes, 2026-11-26 |
| C-5 | Single user, no logins. | Accepted default |
| C-6 | Output is used in a **Unity** project. | Problem statement |

Note: GitHub Pages serves static sites. Several requirements (keeping credentials secret, storing images, enforcing the budget) need something that is not static. This doesn't change the requirements, but it must be resolved in the design step (OQ-1).

## 9. Dependencies

| ID | Dependency | Needed for |
|---|---|---|
| D-1 | Azure subscription with a Microsoft Foundry deployment of a MAI-Image model. | FR-6, FR-8 |
| D-2 | A text model for prompt expansion (which one is open, OQ-8). | FR-5, FR-7 |
| D-3 | Unity project on the user's machine (Unity version open, OQ-9). | FR-13 |
| D-4 | GitHub repository and GitHub Pages. | C-2 |
| D-5 | Azure pricing for the chosen models, to compute cost per generation. | FR-11 |

## 10. Assumptions

| ID | Assumption |
|---|---|
| A-1 | Hannes has, or can get, an Azure subscription with access to an image model and a text model in Foundry. |
| A-2 | Asset Forge improves consistency through the style guide, prompting and user review. It does not guarantee identical style; user approval remains the quality gate. |
| A-3 | The game's art is stylised 2D (painterly or clean cartoon), not strict pixel art. |
| A-4 | Images are for a game intended to ship, so provenance (model and settings) is recorded per image. |
| A-5 | Volume is tens to a few hundred images per week. |
| A-6 | The Unity side needs only image files and import settings, not a full editor plugin with its own UI. |
| A-7 | Generating above the asset's target size and downscaling is acceptable (MAI-Image minimum output is 768 × 768). |

## 11. Success metrics

| ID | Metric | Target | How measured |
|---|---|---|---|
| SM-1 | Share of variants that match the style guide without prompt edits | ≥ 3 of 4 | User approval rate for requests where the expanded prompt was not edited (measurement method open, OQ-16). |
| SM-2 | Time from request to approved asset in Unity | < 5 min | From request submission to export completion. |
| SM-3 | Monthly spend | ≤ €100 | Recorded generation costs (FR-11). |

## 12. Release scope (v1)

Must: FR-1 to FR-6, FR-9 to FR-11, FR-12a, FR-13.
Should: FR-7.
Could: FR-8, FR-12b.

## 13. Open questions

| ID | Question | Affects |
|---|---|---|
| OQ-1 | GitHub Pages only serves static content. Where will the parts that need a server live (holding Azure credentials, storing images and metadata, enforcing the budget)? Is a non-static Azure component acceptable alongside Pages? | C-2, NFR-5, FR-10, FR-11 |
| OQ-2 | A GitHub Pages site is publicly reachable (private Pages requires GitHub Enterprise Cloud). With "no logins", how should access be limited to the owner so nobody else can spend the budget? | NFR-6 |
| OQ-3 | How should exported files reach the Unity project on the local machine (e.g. download, sync, direct write)? | FR-13 |
| OQ-4 | Which MAI-Image model? Only MAI-Image-2.7 / 2.7-Flash list transparent backgrounds, and both are **Preview** (no SLA). Is a preview model acceptable for v1? | FR-6, C-3 |
| OQ-5 | If the chosen model cannot produce transparency, is background removal acceptable as a substitute? | FR-6 |
| OQ-6 | What is the per-image cost of the chosen model in EUR, and how is USD pricing converted? Does the €100 include prompt-expansion (text model) costs? | FR-11 |
| OQ-7 | Is €100 a budget per project, or one overall cap across all projects? | FR-11 |
| OQ-8 | Which text model will expand prompts, and is it available in your Foundry model list? | FR-5, D-2 |
| OQ-9 | Unity version; pixels per unit and filter mode per asset type; file naming convention. | FR-13 |
| OQ-10 | Can presets be edited per project, or are they fixed? | FR-4 |
| OQ-11 | Is there a storage limit, and should rejected images ever be deleted automatically? | FR-10, NFR-7 |
| OQ-12 | Should reference images from the style guide be sent to the model for every generation (MAI-Image edits accept up to 5 reference images), or only used as guidance for the prompt? | FR-2, FR-6, cost |
| OQ-13 | Are tiles always square, and which tile size is the default (256 or 512)? | FR-4 |
| OQ-14 | What is the exact pixel size for backgrounds (e.g. 1920 × 1080)? | FR-4, FR-12 |
| OQ-15 | What generation latency per request is acceptable? | NFR-2 |
| OQ-16 | How is "on-style" judged for SM-1: only by approval, or with an explicit rating? | SM-1 |
| OQ-17 | Do MAI-Image terms of use allow commercial use of generated images in a shipped game? | A-4 |
| OQ-18 | For props: trim to content then re-centre on a 512 px canvas, or output the trimmed size? | FR-12 |
| OQ-19 | Can a project be deleted or archived, and what happens to its images? | FR-1 |
| OQ-20 | What should happen when a request is made before the style guide is filled in: block, or generate with whatever is set? | FR-5 |
| OQ-21 | Can an approved (not yet exported) image be un-approved? What happens when an already exported image is exported again (overwrite, skip, new name)? | FR-9, FR-13 |
| OQ-22 | Should past months' spend stay viewable? | FR-11 |

---

## Appendix A: MAI-Image model evaluation

Research done 2026-11-26 from Microsoft documentation. This informs C-3 and the open questions; the final choice is made in the design step.

| Model | Status | Text-to-image | Image edits (up to 5 reference images) | Transparent background |
|---|---|---|---|---|
| MAI-Image-2.7 | Preview | Yes | Yes | **Yes** |
| MAI-Image-2.7-Flash | Preview | Yes | Yes | **Yes** (same capabilities as 2.7, faster and cheaper) |
| MAI-Image-2.6 | Released (2026-07-31) | Yes | Yes | Not listed |
| MAI-Image-2.6-Flash | Released (2026-07-31) | Yes | Yes | Not listed |
| MAI-Image-2.5 / 2.5-Flash / 2.5-Pro | Preview | Yes | Yes | Not listed |

Facts relevant to the requirements:

- **Output** is always PNG. Width and height must each be **at least 768 px**. Maximum is about 1536 × 1536 total pixels for 2.6 models and 1024 × 1024 for 2.5 models. So 256 px icons and 512 px props must be downscaled (FR-12a, A-7).
- **Multi-reference editing** (up to 5 images per request) can feed style guide reference images to the model, which directly supports consistency (G1, OQ-12).
- **Preview models** have no SLA and Microsoft does not recommend them for production (OQ-4).
- **Pricing** is token-based. Published for MAI-Image-2.6: $5 / 1M text input tokens, $8 / 1M image input tokens, $38 / 1M image output tokens; 2.6-Flash about half of that. Third-party reports put MAI-Image-2.7-Flash at around $10 per 1,000 images; not confirmed from Microsoft pricing (OQ-6).

Initial read: **MAI-Image-2.7-Flash** best matches the requirements (transparency, reference images, low cost), with the risk that it is in preview. MAI-Image-2.6 is the non-preview fallback but would need background removal (OQ-5).

Sources:
- [Deploy and use MAI image models in Microsoft Foundry (Microsoft Learn)](https://learn.microsoft.com/en-us/azure/foundry/foundry-models/how-to/use-foundry-models-mai-image)
- [MAI-Image-2.7 model card (Foundry catalog)](https://ai.azure.com/catalog/models/MAI-Image-2.7)
- [MAI-Image-2.6 and 2.6-Flash announcement (Microsoft Tech Community)](https://techcommunity.microsoft.com/blog/azure-ai-foundry-blog/mai-image-2-6-and-mai-image-2-6-flash-quality-and-speed-at-production-scale/4550970)
- [MAI Image 2.7 Flash pricing report (third party)](https://superpowerdaily.com/posts/microsoft-releases-mai-image-2-7-with-flash-priced-around-10-per-1-000-images)
