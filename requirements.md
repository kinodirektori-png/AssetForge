# Asset Forge – Requirements

| | |
|---|---|
| **Status** | Draft v0.2 for review |
| **Owner** | Project owner |
| **Date** | 2026-11-26 |
| **Previous version** | v0.1 (2026-11-26). Changes are listed in [§15](#15-change-log). |
| **Scope of this document** | *What* the system must do and *how well*. *How* it is built (architecture, components, storage technology) is decided in a later step. |

Requirement keywords: **Must** = required for v1, **Should** = wanted for v1 but can slip, **Could** = nice-to-have, **Later** = not in v1.
Items marked **DEC-n** refer to the owner's [decisions](#13-decisions).
Items marked **OQ-n** refer to [Open questions](#14-open-questions).

---

## 1. Problem statement

The project owner is building a Unity game that needs many 2D images: props, tiles, icons and backgrounds. There is no access to a good general image generator. Free tools give inconsistent results: each image comes out in a different style, perspective or palette, and time is wasted rewriting prompts by hand.

## 2. Product summary

Asset Forge is a small single-user app that runs on the owner's computer. It keeps a per-project style guide and uses it to turn short requests (for example "mossy stone door") into detailed, consistent image prompts. It generates variants through an Azure-hosted image model, stores them, and lets the user approve the ones to export to a folder on their computer.

## 3. Goals and non-goals

### 3.1 Goals

- **G1 Consistency.** Images generated for one project share the same style, perspective and palette without the user rewriting prompts. Whether this is achievable is checked in Phase 0 ([§12](#12-release-scope)) before the product is built.
- **G2 Speed.** Going from a short request to an approved, exported image is fast (target in [§11](#11-success-metrics)).
- **G3 Low effort.** The user types a short request; the system supplies the detail from the style guide and asset-type rules.
- **G4 Control.** The user decides which images are used; nothing is exported without approval.
- **G5 Cost control.** Asset Forge never spends more than a fixed monthly cap. If the app fails or a credential leaks, controls outside the app limit the overspend but cannot fully prevent it (NFR-3).
- **G6 Traceability.** For every image it is known how it was made (request, prompt, style guide and preset version, models, settings, post-processing, cost), with enough detail to support a store's AI-content disclosure.
- **G7 Privacy.** Only the owner can use the tool, spend the budget, or see the stored images.

### 3.2 Non-goals (out of scope for v1)

- Animation frames and sprite sheets.
- 3D assets.
- Fine-tuning or training a style model.
- Multiple users, or use by anyone other than the owner (including anonymous public access).
- Editing parts of an image inside Asset Forge (inpainting / region editing as a user feature).
- Variations of an approved image (former FR-8, moved to Later).
- Building sprite atlases (left to Unity).
- Strict pixel-art output (see assumption A-3).

## 4. Users and key scenarios

### 4.1 Users

| User | Description |
|---|---|
| Game developer (single user) | The project owner. Builds the Unity game, owns the art direction, approves every asset. Only user of v1. |

### 4.2 Key scenarios

- **S1 Set up a project.** The user creates a project for a game, fills in its style guide (description, perspective, palette, lighting, outlines, detail level, things to avoid, reference images) and saves it. A partly filled guide can be saved.
- **S2 Request an asset.** The user picks an asset type (for example *prop*, size *medium*), types "mossy stone door", reviews the expanded prompt, optionally edits it, and generates 4 variants. Whatever style guide fields are set are used.
- **S3 Review and approve.** The user looks at the variants in a gallery, approves the good ones and rejects the rest. A wrong click can be undone.
- **S4 Iterate.** None of the variants is right; the user regenerates with short feedback ("more moss, less glow"), checks the updated prompt and gets new variants.
- **S5 Export.** The user exports approved images, selects a folder on their computer, and the images are saved there.
- **S6 Watch spending.** The user sees how much of this month's budget is used, is warned before it runs out, and generation stops at the cap.
- **S7 Evolve the style.** The user changes the style guide; older images remain traceable to the style guide version that produced them.
- **S8 Come back later.** The user starts a generation and closes the Asset Forge window; the generation carries on, and the user later finds the finished variants and their recorded cost. If the computer shuts down mid-generation, nothing that may have been paid for is lost track of.

## 5. Domain terms

| Term | Meaning |
|---|---|
| Project | One game (or prototype). Owns one style guide, its requests and images. |
| Style guide | Project-wide visual rules, versioned. |
| Asset type | One of: *prop*, *tile*, *icon*, *background*. |
| Preset | Fixed extra rules for an asset type, versioned. |
| Size class | For props only: *small*, *medium* or *large*. Sets how tall the object is on the canvas so props keep their relative scale. |
| Request | A short description from the user plus an asset type (and a size class for props). |
| Expanded prompt | The prompt built from the request: a *fixed part* made by a template from the style guide and preset, plus a *subject part* written by the text model from the request text. |
| Round | One generation of up to 4 variants for a request: the first one, then one per feedback. |
| Variant | One generated image. |
| Review status | *Pending review*, *Approved* or *Rejected* (FR-9). |
| Export record | Path, file hash and time of one export of an image. An image with at least one export record is shown as *Exported*. |
| Budget cap | The single monthly spending limit across all projects (DEC-2). |
| Committed spend | Recorded cost of finished calls this month plus the maximum estimated cost of calls still running. |

## 6. Functional requirements

Each requirement has acceptance criteria in Given / When / Then form.

### FR-1 Projects

- **FR-1a — Must.** The user must be able to create a project, open it and edit it.
- **FR-1b — Should.** The user should be able to have several projects: list them, switch between them and rename them. Each project's style guide, requests and images are kept apart.

Project names must be 1 to 50 characters and unique (ignoring case). Each project gets a short identifier (slug) at creation that is used in export file names (FR-13). Renaming a project does not change its slug or rename exported files.

- **AC-1.1** Given no projects exist, when the user creates a project named "Dungeon", then "Dungeon" appears with an empty style guide.
- **AC-1.2** Given two projects "Dungeon" and "Forest", when the user opens "Forest", then only Forest's style guide, requests and images are shown.
- **AC-1.3** Given a project "Dungeon", when the user renames it to "Crypt", then the project list shows "Crypt" and its style guide, images and export folder are unchanged.
- **AC-1.4** Given a project "Dungeon" exists, when the user creates a project named "dungeon", then it is refused with a message that the name is already used.

### FR-2 Style guide — Must

Each project must have a style guide with these fields:

| Field | Content | Limit |
|---|---|---|
| Style description | Short free text, e.g. "stylised hand-painted, soft shading". | ≤ 300 characters |
| Camera / perspective | e.g. "¾ top-down". | ≤ 100 characters |
| Palette | Colours as hex values. | Up to 16 colours (DEC-4); may be empty (DEC-6) |
| Lighting direction | e.g. "light from top-left". | ≤ 100 characters |
| Outline weight | e.g. "thin dark outline". | ≤ 100 characters |
| Detail level | e.g. "medium". | ≤ 100 characters |
| Things to avoid | List of terms that must not appear (e.g. "photorealism, text, watermark"). | ≤ 20 terms, each ≤ 40 characters |
| Reference images | Images that show the target style. | 0 to 3; PNG or JPEG; ≤ 10 MB and ≤ 4096 px per side each |

- A style guide can be saved with any field empty. Expansion and generation use whatever fields are set (DEC-6).
- Reference image files are checked before they are processed. A file whose declared size is over the limit is refused without being fully decoded. Stored copies are re-encoded and have all metadata (EXIF, GPS, text chunks) removed.
- When adding a reference image, the user confirms they have the right to use it as a reference (A-9).

- **AC-2.1** Given a project, when the user enters all style guide fields and saves, then reopening the project shows the same values.
- **AC-2.2** Given the user enters 17 palette colours, when they save, then the save is refused with a message that at most 16 colours are allowed.
- **AC-2.3** Given the user enters a palette value that is not a valid hex colour, when they save, then the save is refused and the invalid value is pointed out.
- **AC-2.4** Given a style guide with 3 reference images, when the user tries to add a fourth, then it is refused with a message that the maximum is 3.
- **AC-2.5** Given a field value longer than its limit, when the user saves, then the save is refused, naming the field and its limit.
- **AC-2.6** Given a GIF file, a 12 MB PNG, or a JPEG 5000 px wide, when the user adds it as a reference image, then it is refused with a message stating the allowed types and sizes.
- **AC-2.7** Given a JPEG with GPS data in its EXIF, when it is added as a reference image, then the stored copy contains no EXIF or GPS data.
- **AC-2.8** Given a style guide without a palette, when the user saves and then asks for expansion, then the guide is saved and the expanded prompt contains the fields that are set and no palette text.

### FR-3 Style guide versioning — Must

Every save that changes at least one value must create a new style guide version. A save without changes does not create a version. Earlier versions remain viewable.

- **AC-3.1** Given a style guide at version 1, when the user changes the palette and saves, then the style guide is at version 2 and version 1 is still viewable with its original palette.
- **AC-3.2** Given images generated under version 1, when the style guide moves to version 2, then those images still show "style guide v1" in their details.
- **AC-3.3** Given a style guide at version 2, when the user saves without changing anything, then it stays at version 2.
- **AC-3.4** Given a prompt was expanded under version 2, when the style guide moves to version 3 before the user generates, then the user is warned and can re-expand or generate with the prompt as shown; the image records the version used for expansion.

### FR-4 Asset-type presets — Must

Each asset type must have a preset of rules that is applied on top of the project style guide:

| Asset type | Preset rules | Output size |
|---|---|---|
| Prop | One object, transparent background, standing on the bottom of the canvas. Object height set by size class (FR-12). | 512 × 512 px |
| Tile | Square, seamless on all four edges (FR-12), same perspective as the project. | 256 × 256 or 512 × 512 px, set per project (default 256, OQ-13) |
| Icon | One subject, strong silhouette, transparent background, centred. | 256 × 256 px |
| Background | 16:9, no characters. | Set per project (default 1920 × 1080, OQ-14) |

Presets are fixed in v1 and cannot be edited per project (OQ-10). Each preset has a version, which is recorded with every image (FR-10).

When the image model cannot produce the output size directly (see [Appendix A](#appendix-a-mai-image-model-evaluation)), the system generates at the nearest supported size with the same aspect ratio and resizes (FR-12). Upscaling is allowed for backgrounds and is recorded.

- **AC-4.1** Given asset type *icon*, when the system builds the expanded prompt, then the prompt includes the icon rules (strong silhouette, transparent background) as well as the project style guide.
- **AC-4.2** Given asset type *background*, when the system builds the expanded prompt, then the prompt includes "no characters" and a 16:9 format.
- **AC-4.3** Given a project whose tile size is 256 px, when the user requests a tile, then it is delivered at 256 × 256 px; when the tile size setting is changed, then only new requests use the new size.
- **AC-4.4** Given asset type *prop*, when the user creates a request, then they choose a size class (default *medium*).

### FR-5 Request and prompt expansion — Must

The user must be able to submit a short request (free text plus asset type). The system must expand it into a prompt in two parts:

- **Fixed part:** built by a template from every filled style guide field and the asset-type preset. The same inputs always give the same fixed part. The template has a version.
- **Subject part:** written by the text model (D-2) from the request text.

The things-to-avoid list is sent as a negative prompt where the model supports one. MAI-Image documents no negative-prompt parameter (Appendix A), so for now the list is included in the prompt, and Phase 0 measures whether that helps or hurts ([§12](#12-release-scope)).

The expanded prompt must be shown to the user before generation and must be editable. Its length is shown while editing and must not exceed the image model's limit (32,000 tokens for MAI-Image). The final prompt actually sent must be saved with every image, together with the original expansion and a flag saying whether the user edited it.

- **AC-5.1** Given a project with a complete style guide and the request "mossy stone door" of type *prop*, when the user asks for expansion, then the shown prompt contains the request and, word for word, the style guide's description, perspective, palette, lighting, outline and detail values, its things-to-avoid terms (unless sent as a negative prompt), and the prop preset rules. Empty optional fields leave no placeholder text.
- **AC-5.2** Given the same request and the same style guide version, when the user expands it twice, then the fixed part is identical both times.
- **AC-5.3** Given an expanded prompt is shown, when the user edits it and generates, then the edited text is what is sent to the image model and saved with each variant, the variant is marked "prompt edited", and the original expansion is kept.
- **AC-5.4** Given an expanded prompt is shown, when the user generates without editing, then the expansion is sent unchanged and the variant is not marked "prompt edited".
- **AC-5.5** Given an edited prompt longer than the model's limit, when the user tries to generate, then it is refused, showing the length and the limit.
- **AC-5.6** Given the text model fails or times out, when the user asks for expansion, then the user is told, the request text is kept, expansion can be retried, and no image model call is made.

### FR-6 Variant generation — Must

For each round the system must generate 4 variants using the Azure-hosted image model (C-3).

- Closing the Asset Forge window must not stop a running generation; its results are stored when it finishes (S8).
- If Asset Forge itself is stopped mid-generation (shutdown, crash), then on the next start every unfinished variant is shown as interrupted, its maximum estimated cost stays counted in spend, and it can be retried.
- Each variant is attempted on its own; one failing does not affect the others.
- Temporary errors (rate limiting, timeouts, server errors) are retried automatically, at most 3 times per variant with increasing delay and within 3 minutes in total. Other errors are not retried automatically.
- Every call that may have been billed is recorded in spend (FR-11), and any image it returns is stored, even if it arrives after the variant was marked failed.
- Calls refused by the model's content filter are reported as their own error type, not as a general failure.
- Props and icons must be delivered with a transparent background: from the model's own transparency output where available, otherwise by background removal (DEC-3, NFR-9). *Transparent background* means: every pixel in the outer 2 % of the canvas on each side has alpha 0, and the image has at least one fully opaque pixel.
- Whether the style guide reference images are sent to the model with each generation is decided in Phase 0 (OQ-12). With MAI-Image, sending them means using the image-edit endpoint, which takes up to 5 JPEG or PNG images (Appendix A). If they are sent, which ones were sent is recorded (FR-10).

- **AC-6.1** Given an approved prompt and enough budget, when the user generates, then 4 variants appear for review with status *Pending review*.
- **AC-6.2** Given a request of type *prop* or *icon*, when variants are generated, then each delivered PNG has a transparent background as defined above.
- **AC-6.3** Given the image model returns an error for one or more variants, when generation finishes, then the user sees which variants failed and why, the successful ones are kept, and a retry runs only the failed variants.
- **AC-6.4** Given the budget does not allow the round (FR-11), when the user tries to generate, then no call is made to the image model and the user is told why.
- **AC-6.5** Given a generation has started, when the user closes the Asset Forge window and opens it again later, then every variant that completed is shown and its cost is recorded.
- **AC-6.6** Given the model rate-limits one variant, when the limit clears within the retry window, then the user sees 4 variants without having to do anything.
- **AC-6.7** Given the content filter refuses a prompt, when generation finishes, then the affected variants show "Blocked by content filter", are not retried automatically, and the prompt stays editable for another attempt.
- **AC-6.8** Given the model returns a *prop* with an opaque background, when the image is delivered, then it has a transparent background and its details show that background removal was applied.
- **AC-6.9** Given Asset Forge is stopped while a variant's model call is running, when it starts again, then that variant is shown as interrupted, its maximum estimated cost is still counted in spend, and the user can retry it.

### FR-7 Iteration with feedback — Should

The user should be able to regenerate a request with short feedback text. The text model receives the previous round's subject part and the feedback, and writes a new subject part, so each round's feedback builds on the earlier rounds. The fixed part is rebuilt by the template (FR-5) and is never changed by the text model or by feedback. If the user hand-edited the previous prompt, they are told those edits are not carried over. The new prompt is shown, editable, as in FR-5. New variants are listed under the same request.

- **AC-7.1** Given a request with 4 variants, when the user enters "more moss, less glow" and regenerates, then an updated prompt is shown for review; after generation, 4 new variants are listed under the same request, and their saved prompt and feedback are recorded.
- **AC-7.2** Given several rounds on one request, when the user views the request, then every round's variants, feedback and prompt are visible in order.
- **AC-7.3** Given a round with feedback "more moss", when the user gives the feedback "less glow", then the shown prompt reflects both and can be edited before sending.
- **AC-7.4** Given the feedback "make it blue and photorealistic", when the new prompt is built, then its fixed part is identical to the template output for the same style guide version; only the subject part changes.

### FR-8 Variations of an approved image — Later

Moved out of v1 (DEC-8). It relies on the model's image-editing feature, which is close to the non-goal "editing parts of an image", and it is not needed to show the core value.

### FR-9 Review gallery — Must

The system must show the images of a project in a gallery where the user can approve or reject each image, mark whether it is on-style, and regenerate a request.

Review status changes:

| From | To | Allowed | Note |
|---|---|---|---|
| Pending review | Approved / Rejected | Yes | |
| Approved | Rejected | Yes | If the image has been exported, the user is warned that the exported file is not removed. |
| Rejected | Approved | Yes | |
| Approved / Rejected | Pending review | No | |

- The gallery shows each image's thumbnail, asset type, request text, review status and an *Exported* marker. It can be filtered by review status, asset type and exported, and is shown in pages.
- **Must:** each variant can be marked on-style yes or no, separately from approval (used for SM-1).
- **Should:** approve or reject several selected images at once; undo the last status change.

- **AC-9.1** Given a project with images in several statuses, when the user opens the gallery, then each image shows its thumbnail, asset type, request text, review status and whether it has been exported.
- **AC-9.2** Given a variant with status *Pending review*, when the user approves it, then its status becomes *Approved*.
- **AC-9.3** Given a variant with status *Pending review*, when the user rejects it, then its status becomes *Rejected*, and it is still stored (see FR-10).
- **AC-9.4** Given the gallery, when the user filters by status, asset type or exported, then only matching images are shown.
- **AC-9.5** Given a rejected image, when the user approves it, then its status becomes *Approved*.
- **AC-9.6** Given an exported image, when the user rejects it, then they are warned that the exported file stays, and after confirming the status becomes *Rejected*.
- **AC-9.7** Given a variant, when the user marks it on-style without approving it, then the on-style mark is stored and its review status is unchanged.
- **AC-9.8 (Should)** Given 4 selected pending images, when the user approves them together, then all 4 become *Approved*.
- **AC-9.9 (Should)** Given the user has just rejected an image by mistake, when they choose undo, then the image returns to its previous status.

### FR-10 Storage and traceability — Must

Every generated image must be stored, including rejected ones, together with:

| Group | Fields |
|---|---|
| Request | Project, request text, asset type, size class or tile size. |
| Prompt | Final prompt sent, original expansion, "prompt edited" flag, feedback for each round. |
| Versions | Style guide version, preset version, expansion template version. |
| Models | Text model name and version; image model name and version, Azure deployment name, API version. |
| Inputs | Generation settings, seed (if the model supports one), identifiers (hashes) of the reference images sent. |
| Processing | Post-processing steps applied (background removal, resize, upscale, trim, tile edge matching) and their settings. |
| Cost | Estimated cost, actual cost, exchange rate used. |
| Lifecycle | Review status history, on-style mark, export records, timestamps (requested, generated, reviewed, exported). |

Both the raw image returned by the model and the processed image are stored. By default nothing is deleted (OQ-11). Stored images and their details are only reachable by the owner (FR-14).

- **AC-10.1** Given any stored image, when the user opens its details, then all fields listed above that apply to it are shown.
- **AC-10.2** Given a rejected image, when the user opens the gallery filtered to *Rejected*, then the image and its details are still available.
- **AC-10.3** Given an image exported last month, when the user looks it up, then they can see exactly which prompt, style guide version, preset version and model produced it.
- **AC-10.4** Given a processed image, when the user opens its details, then the raw model output can be viewed too.

### FR-11 Monthly budget — Must

The system must enforce one monthly budget cap across all projects (DEC-2), default **€100**.

- The cap covers both image model and text model calls.
- Committed spend must never exceed the cap, including when several generations run at the same time.
- Every model call is checked against the cap before it is made, including prompt expansion (FR-5) and feedback rounds (FR-7). A call is made only if committed spend plus its maximum estimated cost fits under the cap; otherwise it is refused and no call is made.
- A round starts only if committed spend plus the maximum estimated cost of the whole round fits under the cap. For a feedback round, that includes its text-model call. If it does not fit, the user is told how many variants would fit and can generate that many instead.
- Each call's cost is recorded: the actual cost from the usage the model reports where available, otherwise the estimate; both are kept. Model prices and the USD→EUR exchange rate are configured values, and the rate used is recorded with each call (OQ-6).
- Calls that fail or are blocked by the content filter count when they are billed (OQ-24).
- A month is a calendar month in UTC.
- A warning is shown at 80 % of the cap.
- **Should:** spend for previous months stays viewable (OQ-22).
- **Could:** per-project limits inside the global cap.
- The cap can only be changed by the owner (FR-14). Lowering it below the current committed spend blocks further generation for the rest of the month.

- **AC-11.1** Given a new installation, when the user opens the budget settings, then the monthly cap is €100 and can be changed.
- **AC-11.2** Given image and text model calls, when each one completes, then its cost is recorded and the month's spend increases by that amount.
- **AC-11.3** Given spend reaches 80 % of the cap, when the user next views any project or generates, then a warning shows the amount spent and remaining.
- **AC-11.4** Given €3 remaining and a round whose maximum estimated cost is €4, when the user tries to generate, then no model call is made and the user is told how many variants fit.
- **AC-11.5** Given room in the budget for only one round, when two rounds are started at the same moment, then exactly one runs and the other is refused.
- **AC-11.6** Given a new calendar month starts (UTC), when the user views the budget, then spend for the new month starts at €0.
- **AC-11.7** Given spend of €90 this month, when the user creates a new project, then the remaining budget is still €10.
- **AC-11.8** Given spend of €60, when the user lowers the cap to €50, then new generations are refused for the rest of the month.
- **AC-11.9** Given a call fails but is billed, when it finishes, then its cost is added to the month's spend.
- **AC-11.10** Given the cap is reached, when the user asks for a prompt expansion, then it is refused with a message that the budget is reached, and no text model call is made.
- **AC-11.11** Given enough budget for the images of a feedback round but not for its text-model call as well, when the user regenerates with feedback, then the round is refused and no model call is made.

### FR-12 Post-processing — Must / Could

**FR-12a (Must).** Delivered images must be processed for their asset type:

- **All types:** resized to the output size (FR-4) using resampling that handles transparency correctly. Semi-transparent edge pixels take their colour from the object, not from the removed background, so there is no light or dark fringe.
- **Props:** transparent margin trimmed; object scaled so its height matches its size class, or less if it would otherwise be wider than 480 px; centred horizontally; bottom edge 16 px above the bottom of the 512 px canvas. Proposed size-class heights: small 160 px, medium 320 px, large 480 px (OQ-25).
- **Icons:** transparent margin trimmed; scaled to fit 240 × 240 px; centred on the 256 px canvas.
- **Tiles:** opposite edges (left/right, top/bottom) made to match so the tile repeats without a visible seam.
- **Backgrounds:** generated at the nearest supported 16:9 size and resized or upscaled to the project's background size.
- Each step and its settings are recorded (FR-10); the raw model output is kept.

**FR-12b (Could).** Tiles where a visible seam remains after processing could be flagged with a warning.

- **AC-12.1** Given the model returns a 1024 × 1024 image for an *icon*, when the image is delivered, then it is 256 × 256 px.
- **AC-12.2** Given a *medium* prop with empty transparent margins, when the image is delivered, then the object is 320 px tall (unless limited by width), horizontally centred, and its bottom is 16 px above the bottom of the 512 px canvas.
- **AC-12.3** Given a *small* prop "cup" and a *large* prop "door", when both are delivered, then the door is 3 times as tall as the cup (unless limited by width).
- **AC-12.4** Given a delivered tile, when its left edge column is compared with its right edge column, and its top row with its bottom row, then for each pair the mean absolute difference over all pixels and all four channels (R, G, B, alpha) is at most 5 on the 0–255 scale (about 2 %).
- **AC-12.5** Given a delivered prop or icon, when every partly transparent pixel (alpha 1 to 254) is compared with the nearest fully opaque pixel, then each of its colour channels (R, G, B) differs by at most 26 on the 0–255 scale (about 10 %).
- **AC-12.6 (Could)** Given a tile with a remaining seam, when it is delivered, then the user sees a seam warning on that tile.

### FR-13 Export to a local folder — Must

The user must be able to export approved images to a folder on their computer:

- When exporting, the user selects the folder. Any folder the user can write to is allowed. Asset Forge remembers the last folder used for each project and offers it next time.
- Images are saved as PNG files directly in the selected folder. Nothing is written outside it.
- File names follow `<project-slug>_<type>_<request-slug>_<short-id>.png`, using only lowercase letters, digits, `-` and `_`, at most 80 characters.
- Exporting an image again to the same folder overwrites the same file.
- Each export creates an export record (FR-10).

- **AC-13.1** Given 3 approved props, when the user exports and selects a folder, then 3 PNG files appear in that folder and the images are shown as *Exported*.
- **AC-13.2** Given a rejected or pending image, when the user exports, then it is not exported.
- **AC-13.3** Given the user exported to a folder last time, when they export again, then that folder is offered first, and the files go to whichever folder they select.
- **AC-13.4** Given the user cancels the folder selection, when exporting, then nothing is saved and no export record is created.
- **AC-13.5** Given an image already exported to a folder, when it is exported again to the same folder, then the file is overwritten and no second copy is created.
- **AC-13.6** Given the request text `../door: "big"?`, when the image is exported, then the file name contains only allowed characters and the file is saved directly in the selected folder.
- **AC-13.7** Given projects "Dungeon" and "Forest" each export a prop for the request "door" to the same folder, when both are exported, then two different files exist and neither overwrote the other.
- **AC-13.8** Given the selected folder is read-only or no longer exists, when the user exports, then they are told which images could not be saved, and those images are not marked *Exported*.

### FR-14 Access control — Must

Only the owner can use any function, spend the budget, or view stored images and their details. Asset Forge runs on the owner's computer (DEC-1) and must:

- accept connections only from that computer;
- if its user interface is served by a local web server, refuse requests that come from other websites open in the owner's browser (cross-site requests, including DNS rebinding).

A refused request is refused before any model call or data access.

- **AC-14.1** Given a request from another device on the network, when it asks to expand or generate, then it is refused, no model call is made, and spend is unchanged.
- **AC-14.2** Given a direct link to a stored image, when it is opened from another device, then it is refused.
- **AC-14.3** Given a web page from another site open in the owner's browser, when it sends a request to Asset Forge's local address, then it is refused, no model call is made, and spend is unchanged.

### FR-15 Provenance export — Should

The user should be able to export a list (CSV) of all exported images with file name, project, model name and version, final prompt and dates, to support a store's AI-content disclosure (A-4).

- **AC-15.1** Given 10 exported images, when the user exports the provenance list, then it has 10 rows with all the listed columns filled.

### FR-16 Operation log — Should

Each model call should be logged with time, project, model, duration, outcome and cost. The log never contains credentials or keys.

- **AC-16.1** Given a day of use, when the user filters the log by outcome "failed", then only failed calls are shown with their error type.
- **AC-16.2** Given any log entry, when it is checked, then it contains no credential, key or access token.

## 7. Non-functional requirements

| ID | Area | Requirement | Target / measure |
|---|---|---|---|
| NFR-1 | Consistency | Variants follow the style guide without manual prompt editing. | Provisional: ≥ 3 of 4 variants marked on-style for unedited prompts (SM-1). Final target set from Phase 0 results. |
| NFR-2 | Responsiveness | System steps are fast enough for the < 5 min goal (SM-2). | Expansion p95 ≤ 10 s. A round of 4 variants p95 ≤ 90 s (provisional until Phase 0, OQ-15). Export of up to 20 images ≤ 10 s. A gallery page loads in ≤ 2 s with one year of images stored at the NFR-10 volume (about 65,000). |
| NFR-3 | Cost | Asset Forge's own spending is capped. If the app's control fails or a credential leaks, the overspend is limited and the owner is told. | Committed spend ≤ cap at all times (FR-11). Outside the app: (a) key authentication is disabled on the Azure model resource, if Azure allows it (OQ-27), so only the owner's Azure sign-in can call it; (b) the image deployment's rate quota is set to the lowest value that still meets NFR-2 (this limits speed only: even the lowest documented quota allows about 259,000 images a month); (c) an Azure budget at the cap alerts the owner and runs an action that disables the model deployments. Azure budgets only alert by themselves, and cost data reaches Azure hours late, so these limit overspend rather than prevent it: the worst case is about quota rate × reporting delay × price per image, recorded as an accepted risk once Phase 0 measures it (OQ-27). Recorded spend is within 5 % of the Azure-billed cost (SM-3). |
| NFR-4 | Traceability | Every image can be traced to how it was made. | 100 % of stored images have all FR-10 fields that apply to them. |
| NFR-5 | Secrets | Azure credentials are never exposed. | Credentials do not appear in the repository, the user interface or anything sent to it, logs, stored metadata or error messages. Asset Forge uses the owner's own Azure sign-in where possible; any key that is needed is kept in the operating system's protected credential store, never in a plain-text config file, and can be rotated without a code change. The identity used can only call the models Asset Forge needs. Secret scanning is enabled on the repository. |
| NFR-6 | Confidentiality | Stored images, prompts and metadata are private. | Nothing is reachable from other devices or other websites (FR-14). Stored files live in the owner's user profile or a folder they choose. |
| NFR-7 | Data safety | Stored images and history are not lost. | No loss across restarts, crashes or app updates. A daily backup to a second location (another drive or cloud storage) limits data loss from a disk failure to at most 24 h. One restore is tested before the v1 release. |
| NFR-8 | Resilience | Model errors and interruptions don't lose work or money. | A failed or interrupted generation leaves the request, prompt and completed variants intact and retryable; a generation continues when the user closes the window, and is shown as interrupted after a shutdown or crash (FR-6). |
| NFR-9 | Portability of model | The image model can be replaced later without changing the user workflow. | Replacing the model requires no change to FR-1 to FR-16 behaviour. Model-specific limits (sizes, prompt length, negative prompt, reference images, native transparency, prices) are configuration, not code. Transparency falls back to background removal. |
| NFR-10 | Volume | Handles the expected load. | Up to 300 requests per week (about 1,200 variants per week, about 5,200 per month). Storage for raw and processed images up to 200 GB in the first year. Provisional: the spend at this volume must fit the cap, which Phase 0 checks (§12). |
| NFR-11 | Upload safety | Uploaded files cannot harm the system. | Uploads are checked before processing (FR-2); oversized files are refused without being fully decoded; stored copies have metadata removed. |

## 8. Constraints

| ID | Constraint | Source |
|---|---|---|
| C-1 | Codebase in **.NET**. | Project owner, 2026-11-26 |
| C-2 | Runs as a **local app on the owner's computer** (DEC-1). Replaces GitHub Pages hosting from v0.1, which serves static files only and could not meet FR-10, FR-11, FR-14 or NFR-5. | Project owner, 2026-11-26; changed in v0.2 |
| C-3 | Images generated by an **Azure-hosted image model**; gpt-image is not in the available Foundry model list, so **MAI-Image** models are the candidates (see [Appendix A](#appendix-a-mai-image-model-evaluation)). Model choice and use of preview models: DEC-3. | Project owner, 2026-11-26 |
| C-4 | Monthly generation budget **€100**, one cap across all projects (DEC-2). | Project owner, 2026-11-26 |
| C-5 | Single user; access restricted to the owner (FR-14). Replaces "no logins" from v0.1. | Changed in v0.2 |
| C-6 | Output is used in a **Unity** project. | Problem statement |

## 9. Dependencies

| ID | Dependency | Needed for |
|---|---|---|
| D-1 | Azure subscription with a Microsoft Foundry deployment of a MAI-Image model. | FR-6 |
| D-2 | A text model for prompt expansion (which one is open, OQ-8). | FR-5, FR-7 |
| D-4 | GitHub repository for the source code. | C-1 |
| D-5 | Azure pricing for the chosen models, to compute cost per call. | FR-11 |
| D-6 | A background removal capability, used when the model gives no transparency. | FR-6, FR-12 |
| D-7 | Azure deployment rate quotas, key authentication disabled on the model resource, and a Cost Management budget whose action disables the deployments. | NFR-3 |
## 10. Assumptions

| ID | Assumption |
|---|---|
| A-1 | The project owner has, or can get, an Azure subscription with access to an image model and a text model in Foundry. |
| A-2 | Asset Forge improves consistency through the style guide, prompting and user review. It does not guarantee identical style; user approval remains the quality gate. |
| A-3 | The game's art is stylised 2D (painterly or clean cartoon), not strict pixel art. |
| A-4 | Images are for a game intended to ship, so provenance (model and settings) is recorded per image. Stores may require disclosure of AI-generated content (for example Steam). |
| A-5 | Volume is tens to a few hundred requests per week, each producing up to 4 variants. |
| A-7 | Generating above the asset's target size and downscaling is acceptable (MAI-Image minimum output is 768 × 768). |
| A-8 | A templated prompt, possibly with reference images, gives consistent enough style. Unproven until Phase 0. |
| A-9 | The owner has the right to use the reference images they upload. |

## 11. Success metrics

| ID | Metric | Target | How measured |
|---|---|---|---|
| SM-1 | Share of variants that match the style guide without prompt edits | ≥ 3 of 4 (provisional, set from Phase 0) | On-style marks (FR-9) on variants whose prompt was not edited. |
| SM-2 | Time from request to approved, exported image | < 5 min | Product metric including the user's own time, from the step timestamps in FR-10: request submission to export completion. |
| SM-3 | Monthly spend | ≤ cap; recorded spend within 5 % of Azure-billed cost | Recorded costs (FR-11) compared with the Azure bill each month. |

## 12. Release scope

### Phase 0 – validation (before design sign-off)

A throwaway spike, not part of v1, to test the main assumption (A-8) before building:

- 20 requests (5 per asset type) × 4 variants, in two setups: (a) fixed-template prompt only; (b) template plus style guide reference images.
- Candidate models: MAI-Image-2.6 and MAI-Image-2.6-Flash, both with background removal (GA models only, DEC-3).
- Measure: on-style rate, transparency and edge quality, tile seams, actual cost per image, latency, whether the things-to-avoid list helps, negative prompt support, prompt length limit, content filter behaviour and billing, seed support.
- Expected cost: well under €20.
- **Exit criteria:** at least one setup reaches ≥ 3 of 4 on-style, **and** that setup's measured cost per variant × expected monthly volume fits under the cap. Cost per variant includes its share of text-model calls and any reference-image input. At the NFR-10 volume (about 5,200 variants a month), €100 allows under €0.02 per variant. If the cost doesn't fit, lower A-5 and NFR-10 to what the cap affords, or raise the cap. Results set NFR-1, pick between the two models and answer OQ-6, OQ-8, OQ-12, OQ-15, OQ-23, OQ-24, OQ-26 and OQ-27. If no setup reaches the target, revisit the product before building it.

### v1

- **Must:** FR-1a, FR-2 to FR-6, FR-9 (except bulk actions and undo), FR-10, FR-11 (except past months and per-project limits), FR-12a, FR-13, FR-14.
- **Should:** FR-1b, FR-7, FR-9 bulk actions and undo, FR-11 past months, FR-15, FR-16.
- **Could:** FR-11 per-project limits, FR-12b.
- **Later:** FR-8.

## 13. Decisions

All decisions below were made by the project owner on 2026-11-26.

| ID | Decision | Options considered | Decided | Why / trade-off | Affects |
|---|---|---|---|---|---|
| DEC-1 | Hosting and access | **A.** Local app on the owner's computer, only reachable from that computer; saves exported files on that computer; uses the owner's Azure sign-in. **B.** GitHub Pages for the UI, plus a server part in Azure with sign-in restricted to the owner. | **A** | Single user and a local Unity project. A removes OQ-1, OQ-2 and OQ-3 and most of the security work; B would add hosting, sign-in, network security and running cost. | C-2, FR-13, FR-14, NFR-5 to NFR-7 |
| DEC-2 | Budget scope | One global cap / one cap per project | **One global cap**; per-project limits as Could | Per-project caps let a new project bypass the limit (N projects = N × €100). | C-4, FR-11 |
| DEC-3 | Image model | Choose after Phase 0 with preview allowed if a GA fallback works / GA models only / preview with no fallback | **GA models only** (MAI-Image-2.6 or 2.6-Flash, both GA per Appendix A; Phase 0 picks). Preview-only parameters are not used. | No dependency on a model without an SLA. Trade-off: transparency always relies on background removal, and cost per image (priced per token) is only known after Phase 0. | C-3, FR-6, §12, OQ-4, OQ-5 |
| DEC-4 | Palette size | 5–8 (v0.1) / 1–16 / 3–12 | **Up to 16 colours** (may be empty, DEC-6) | 5–8 rules out monochrome and limited-palette styles. | FR-2 |
| DEC-5 | Multiple projects in v1 | Must / Should | **Should** (FR-1b) | Not needed to show the core value; one project is enough at first. | FR-1 |
| DEC-6 | Generating before the style guide is complete | Block / warn but allow / generate with what is set | **Generate with what is set** | Owner's choice: no friction. Trade-off: a sparse guide gives less consistent results (G1) and can spend budget on off-style images. | S2, FR-2, FR-5, OQ-20 |
| DEC-7 | Unity integration | Small editor script / no code in Unity / none | **No Unity integration** | Owner's choice. Asset Forge only saves PNG files to a folder the user selects; how they are imported into Unity is outside Asset Forge. | S5, FR-13, OQ-9 |
| DEC-8 | Variations of an approved image (FR-8) | Could in v1 / Later | **Later** | Close to a non-goal and not needed for the core value. | FR-8 |

## 14. Open questions

*Blocks* says what cannot proceed until the question is answered.

| ID | Question | Affects | Status | Blocks | Proposed default |
|---|---|---|---|---|---|
| OQ-1 | Where will the parts that need a server live? | C-2 | Resolved: local app, no server part (DEC-1) | – | – |
| OQ-2 | How is access limited to the owner? | NFR-6 | Resolved: local-only access (FR-14) | – | – |
| OQ-3 | How do exported files reach the Unity project on the local machine? | FR-13 | Resolved: saved to a folder the user selects on their computer (FR-13) | – | – |
| OQ-4 | Which MAI-Image model? Is a preview model acceptable? | FR-6, C-3 | Resolved: GA models only (DEC-3); 2.6 and 2.6-Flash are GA (Appendix A); Phase 0 picks one | – | – |
| OQ-5 | Is background removal acceptable instead of native transparency? | FR-6 | Resolved: yes, required as fallback (FR-6) | – | – |
| OQ-6 | Per-image cost in EUR; USD conversion; text-model costs included? | FR-11 | Partly resolved: text-model costs included; fixed configured rate (FR-11). Cost per image open. | Phase 0 | Measured in Phase 0 |
| OQ-7 | Budget per project or overall? | FR-11 | Moved to DEC-2 | – | – |
| OQ-8 | Which text model expands prompts, and is it in the Foundry model list? | FR-5, D-2 | Open | Phase 0 | – |
| OQ-9 | Unity version; pixels per unit and filter mode per asset type; file naming. | FR-13 | Resolved: Asset Forge has no Unity integration (DEC-7); naming set in FR-13 | – | – |
| OQ-10 | Can presets be edited per project? | FR-4 | Resolved: fixed in v1, versioned | – | – |
| OQ-11 | Storage limit; delete rejected images automatically? | FR-10, NFR-7 | Open | No | Keep everything; plan for ≤ 200 GB in year 1 |
| OQ-12 | Send reference images to the model with every generation? | FR-2, FR-6, cost | Open. The edit endpoint takes up to 5 JPEG or PNG images, but use as style references isn't documented (Appendix A). | Phase 0 | Decided by Phase 0 setup (b) results |
| OQ-13 | Are tiles always square; default tile size? | FR-4 | Resolved with default; owner to confirm | No | Square; project setting; default 256 px |
| OQ-14 | Exact background size? | FR-4, FR-12 | Resolved with default; owner to confirm | No | Project setting; default 1920 × 1080 |
| OQ-15 | Acceptable generation latency? | NFR-2 | Provisional target in NFR-2 | Phase 0 | p95 ≤ 90 s per round |
| OQ-16 | How is "on-style" judged for SM-1? | SM-1 | Resolved: separate on-style mark (FR-9) | – | – |
| OQ-17 | Do MAI-Image terms allow commercial use of generated images in a shipped game? | A-4 | Open | v1 release | – |
| OQ-18 | Prop trim rule? | FR-12 | Resolved: size class, bottom-aligned (FR-12a) | – | – |
| OQ-19 | Can a project be deleted or archived, and what happens to its images? | FR-1 | Open | No | No deletion in v1 |
| OQ-20 | Generate before the style guide is filled in? | FR-5 | Resolved: generate with whatever is set (DEC-6) | – | – |
| OQ-21 | Un-approve; re-export behaviour? | FR-9, FR-13 | Resolved: FR-9 status table; re-export overwrites (FR-13) | – | – |
| OQ-22 | Keep past months' spend viewable? | FR-11 | Resolved: yes (Should) | – | – |
| OQ-23 | Does the chosen model support a negative prompt, and what is its maximum prompt length? | FR-5 | Resolved from docs: no negative-prompt parameter; prompt up to 32,000 tokens (Appendix A). Phase 0 still measures whether listing things to avoid helps. | – | – |
| OQ-24 | Are calls refused by the content filter billed? | FR-6, FR-11 | Open | Phase 0 | Assume billed until confirmed |
| OQ-25 | Are the size-class heights (160 / 320 / 480 px) right for the game? | FR-12 | Open | No | As proposed in FR-12a |
| OQ-26 | Does the chosen model support a seed for repeatable output? | FR-10 | Resolved from docs: no seed parameter (Appendix A) | – | – |
| OQ-27 | What is the lowest rate quota the chosen deployment allows, how long does Azure take to report cost, and can key authentication be switched off on the model resource? Together with price per image, these give the worst-case overspend if the in-app cap is bypassed. | NFR-3 | Partly answered: 6 requests per minute at Tier 1 for 2.6 (about 259,000 images a month), so a quota can't hold spend to the cap. Reporting delay and switching off keys: open. | Phase 0 | – |

## 15. Change log

Changes from v0.1. *Review #* refers to the issue numbers in the v0.1 review of 2026-11-26.

| Review # | Change | Where |
|---|---|---|
| 1 | GitHub Pages hosting no longer assumed; decision with recommendation added. Pages note removed. | C-2, DEC-1 |
| 2 | "No logins" replaced by owner-only access; new privacy goal. | C-5, FR-14, G7, NFR-6 |
| 3 | One global budget cap; new projects don't add budget. | C-4, DEC-2, FR-11, AC-11.7 |
| 4 | Budget defined by committed spend, checked for the whole round before starting; concurrent rounds covered; control outside the app added. | FR-11, AC-11.4, AC-11.5, NFR-3 |
| 5 | Transparency no longer depends on a preview model; background removal fallback; transparency defined. | FR-6, AC-6.2, AC-6.8, NFR-9, DEC-3, D-6 |
| 6 | Unity import handling removed: Asset Forge has no Unity integration (DEC-7). | FR-13, DEC-7 |
| 7 | Open questions given status and what they block; decisions split out with proposed defaults. | §13, §14 |
| 8 | Phase 0 validation spike added before design sign-off; consistency marked as unproven. | §12, A-8, NFR-1, SM-1 |
| 9 | Generation continues when the UI closes; retries, timeouts and billed calls specified. | FR-6, AC-6.5, AC-6.6, S8, NFR-8 |
| 10 | Prompt split into a fixed template part and a subject part; prompt length limit; text model failure. | FR-5, AC-5.1, AC-5.2, AC-5.5, AC-5.6 |
| 11 | Traceability fields extended; version captured at expansion time; saves without changes don't create versions. | FR-10, FR-3, AC-3.3, AC-3.4 |
| 12 | "Prompt edited" flag and original expansion kept; edited prompts left out of SM-1. | FR-5, AC-5.3, SM-1 |
| 13 | Review status table; *Exported* is now an export record, not a status. | §5, FR-9 |
| 14 | Safe file names; no collisions between projects; nothing written outside the selected folder. | FR-13, AC-13.6, AC-13.7 |
| 15 | Tiles made seamless by post-processing (Must), with a measurable check. | FR-12a, AC-12.4 |
| 16 | Output sizes defined against model limits; upscaling allowed for backgrounds. | FR-4 |
| 17 | Prop size classes and bottom alignment keep relative scale. | §5, FR-4, FR-12a, AC-12.2, AC-12.3, OQ-25 |
| 18 | Content filter refusals reported separately; billing question added. | FR-6, AC-6.7, OQ-24 |
| 19 | Reference image uploads checked, re-encoded and stripped of metadata; rights confirmation. | FR-2, AC-2.6, AC-2.7, A-9, NFR-11 |
| 20 | Secrets requirement extended to logs, metadata, error messages, rotation and least privilege. | NFR-5 |
| 21 | Confidentiality of stored data; backups with a 24 h limit on data loss. | NFR-6, NFR-7 |
| 22 | Provenance export for store disclosure; commercial-use question blocks v1 release. | FR-15, A-4, OQ-17 |
| 23 | Latency split into system targets; SM-2 kept as a product metric. | NFR-2, SM-2 |
| 24 | Feedback rounds defined: build on the last prompt sent, shown for editing. | FR-7, AC-7.3 |
| 25 | Style guides can be saved with empty fields; generation uses whatever is set (DEC-6). | FR-2, AC-2.8, DEC-6 |
| 26 | Palette 1–16 colours; length limits on all fields. | FR-2, AC-2.2, AC-2.5, DEC-4 |
| 27 | Months in UTC; configured exchange rate; past months viewable. | FR-11, AC-11.6 |
| 28 | Tile size is a project setting. | FR-4, AC-4.3 |
| 29 | Resizing that handles transparency correctly; no fringes. | FR-12a, AC-12.5 |
| 30 | Volume stated in requests, variants and storage. | NFR-10, A-5 |
| 31 | Variations moved to Later; multiple projects moved to Should. | FR-1, FR-8, DEC-5, DEC-8, §3.2 |
| 32 | Bulk actions, undo and paging in the gallery; gallery speed target. | FR-9, NFR-2 |
| 33 | On-style mark separate from approval. | FR-9, AC-9.7, SM-1 |
| 34 | Operation log for model calls. | FR-16 |
| – | DEC-1 decided by owner: local app. GitHub Pages and server-hosting options removed; added local-only access with cross-site request protection, interrupted generations after shutdown, export to a local folder, local backups. | C-2, DEC-1, S8, FR-6, AC-6.9, FR-13, FR-14, NFR-5 to NFR-8, D-4, OQ-1 to OQ-3 |
| – | DEC-2 to DEC-8 decided by owner. Different from the earlier proposals: GA image models only (DEC-3); generation allowed with an incomplete style guide (DEC-6); no Unity integration (DEC-7). | §13, S1, S2, FR-2, AC-2.2, AC-2.8, FR-13, §12, OQ-4, OQ-20, Appendix A |
| – | Export simplified by owner: the user selects any folder on their computer when exporting and the images are saved there. All Unity dependencies removed from export: fixed `Assets/Art/` paths, Unity-project checks, `.meta`/GUID handling, the import settings table and Unity-specific acceptance criteria. A-6 and D-3 removed; OQ-9 closed. Folder remembered per project; cancelled export and unwritable folder covered. | S5, FR-13, AC-13.1 to AC-13.8, DEC-7, A-6, D-3, OQ-3, OQ-9 |
| – | Cost follow-up review. (1) G5 and NFR-3 no longer promise a hard stop outside the app: Azure budgets only alert and quotas limit rate, not money. NFR-3 now names concrete limits (key authentication off, lowest workable quota, budget action that disables deployments) and treats the worst-case overspend as an accepted risk. (2) Phase 0 must show cost per variant × monthly volume fits the cap (under €0.02 per variant at the NFR-10 volume); otherwise volume is lowered or the cap raised. (3) Every model call, including expansion and feedback text calls, is checked against the cap. | G5, NFR-3, NFR-10, D-7, §12, FR-11, AC-11.10, AC-11.11, OQ-27 |
| – | Consistency review. Fixed merged table rows (D-2/D-4, A-5/A-7). DEC-4 now allows an empty palette, matching FR-2. Project slug added to export file names. FR-7 feedback changes only the subject part, never the template-built part. Gallery speed target sized to a year at NFR-10 volume. AC-12.4 and AC-12.5 given measurable thresholds. Appendix A re-checked against Microsoft docs; 2.6 and 2.6-Flash confirmed GA on the Learn page (only 2.7, 2.7-Flash and 2.5 are marked preview), so DEC-3 stands. Negative prompt, seed, reference images, size limits, prompt length, rate limits, auth and 2.6-Flash pricing recorded. | §9, §10, DEC-4, FR-1, FR-5, FR-6, FR-7, AC-7.4, FR-13, NFR-2, NFR-3, AC-12.4, AC-12.5, §12, OQ-4, OQ-12, OQ-23, OQ-26, OQ-27, Appendix A |

---

## Appendix A: MAI-Image model evaluation

Research done 2026-11-26 from Microsoft documentation, and re-checked the same day against the Learn page (last updated 2026-11-24) and the MAI-Image-2.6 announcement (2026-09-04). It informs C-3, DEC-3 and the open questions.

| Model | Status | Text-to-image | Image edits (up to 5 reference images) | Transparent background |
|---|---|---|---|---|
| MAI-Image-2.7 | Preview | Yes | Yes | **Yes** (documented: `background: "transparent"`) |
| MAI-Image-2.7-Flash | Preview | Yes | Yes | Not confirmed in Microsoft documentation |
| MAI-Image-2.6 | **GA** (version 2026-07-31; see note) | Yes | Yes | Not stated |
| MAI-Image-2.6-Flash | **GA** (version 2026-07-31; see note) | Yes | Yes | Not stated |
| MAI-Image-2.5 / 2.5-Flash / 2.5-Pro | Preview | Yes | Yes | Not stated |

**Status check for 2.6:** the Learn page's model table (updated 2026-11-24) marks 2.7, 2.7-Flash and the 2.5 models "(Preview)" and gives 2.6 and 2.6-Flash no label, so 2.6 and 2.6-Flash are GA. The "(Preview)" next to 2.6 in the page's parameter table applies only to the `auto_aspect_ratio` and `web_grounding` parameters, which Asset Forge does not use. The 2.6 announcement (2026-09-04) still says public preview; the Learn page is newer. DEC-3 ("GA models only") therefore has two candidates: 2.6 and 2.6-Flash.

Facts relevant to the requirements:

- **Output** is always PNG. Width and height must each be **at least 768 px**. Total pixels (width × height) may be at most 2,359,296 for 2.6 models (about 1536 × 1536) and 1,048,576 for 2.5 models. No maximum for a single side and no fixed aspect ratios are documented. 2.6's `auto_aspect_ratio` and `web_grounding` parameters are preview features and are not used; Asset Forge always sets width and height. A 1920 × 1080 background (2,073,600 pixels) fits the 2.6 limit. 256 px icons and 512 px props must be downscaled (FR-12a, A-7).
- **Prompt length:** at most 32,000 tokens (FR-5).
- **Negative prompt:** no such parameter is documented. The documented request parameters are model, prompt, image, width, height, auto_aspect_ratio and web_grounding (FR-5, OQ-23).
- **Seed:** no seed parameter is documented, so output can't be reproduced exactly (FR-10, OQ-26).
- **Reference images:** sent through a separate image-edit endpoint, up to 5 JPEG or PNG images per request. Per-image size limits are not stated. The announcement describes keeping a product, character or brand mark consistent; using reference images as *style* references is not documented, so Phase 0 tests it (G1, OQ-12).
- **Rate limits:** for 2.6 and 2.6-Flash, 6 requests per minute at Tier 1, rising to 36 at Tier 6; the tier depends on the subscription. Even 6 per minute is about 259,000 images a month, so a quota limits speed, not monthly spend (NFR-3, OQ-27).
- **Authentication:** Microsoft Entra ID and API keys are both supported. Whether key authentication can be switched off is not stated on these pages (NFR-3, OQ-27).
- **Preview models** have no SLA and Microsoft does not recommend them for production (DEC-3).
- **Pricing** is per token. MAI-Image-2.6: $5 per 1M text input tokens, $8 per 1M image input tokens, $38 per 1M image output tokens. MAI-Image-2.6-Flash: $1.75, $2.50 and $19 respectively. No per-image price is published (OQ-6). Third-party reports put MAI-Image-2.7-Flash at around $10 per 1,000 images; not confirmed by Microsoft.

Sources:
- [Deploy and use MAI image models in Microsoft Foundry (Microsoft Learn)](https://learn.microsoft.com/en-us/azure/foundry/foundry-models/how-to/use-foundry-models-mai-image)
- [MAI-Image-2.7 model card (Foundry catalog)](https://ai.azure.com/catalog/models/MAI-Image-2.7)
- [MAI-Image-2.6 and 2.6-Flash announcement (Microsoft Tech Community)](https://techcommunity.microsoft.com/blog/azure-ai-foundry-blog/mai-image-2-6-and-mai-image-2-6-flash-quality-and-speed-at-production-scale/4550970)
- [MAI Image 2.7 Flash pricing report (third party)](https://superpowerdaily.com/posts/microsoft-releases-mai-image-2-7-with-flash-priced-around-10-per-1-000-images)
