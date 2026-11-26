---
name: maintain-docs
description: Bring the docs in docs/ (spec, ADRs, getting-started, references to requirements) in line with the code. Use after a feature PR, before opening a PR, or when asked to check or update the docs.
---

# Maintain docs

The docs describe what the code does today and why. This skill finds where they drift from the code and fixes the docs.
It never changes code to match the docs: a real gap in the code is reported, not fixed here.

## The docs and what each one owns

| File | Owns | Changes allowed |
|---|---|---|
| `docs/spec.md` | The MVP: scope, architecture, access protection, endpoints, delivery order | Update to match the code. It is the source of truth for *what is planned*; where the code deliberately differs, record the change (ADR) and update the spec |
| `docs/adr/NNNN-*.md` | One decision each: context, decision, options, consequences | **Never rewrite an accepted record.** Typos and broken links only. A changed decision gets a new record (below) |
| `docs/adr/README.md` | Index of records | Add rows, update the Status column |
| `docs/getting-started.md` | Setup, run, configuration keys, data location, tests | Update to match the code and CI |
| `docs/requirements.md` | Product requirements (*what* and *how well*, no architecture) | Do not edit to match the code. Only fix broken links. If the code contradicts a requirement, report it |
| `docs/decisions-and-mistakes.md` | One line per correction or caught mistake | Append only, when a mistake is found (claude.md) |
| `claude.md` | The owner's working rules | Never edit. Report drift instead |

## Steps

1. **Pick the code to compare against.** Use the branch you are on. If an open PR holds code that is not on `Main` yet,
   say which branch you compared against.
2. **Collect the facts from the code**, not from memory or old PR text. At least:
   - Projects, target framework and build settings: `*.slnx`, `Directory.Build.props`, `src/*/*.csproj`, `tests/*/*.csproj`.
   - NuGet packages and versions in every `.csproj` (compare with spec §8 and ADR 0013).
   - Endpoints: every `Map*` call (`grep -rn "Map\(Get\|Post\|Put\|Patch\|Delete\|Group\)" src`), including public ones.
   - Middleware order and behaviour in `Program.cs` and `src/AssetForge.Api/Security/` (compare with spec §5 and ADR 0004).
   - Configuration keys read by the code (`grep -rn 'config\[\|GetValue\|GetSection\|GetConnectionString' src`) and the
     defaults in `appsettings.json` and `launchSettings.json`.
   - Startup checks, data paths, error handling.
   - CI jobs and their names in `.github/workflows/` (compare with ADR 0012 and getting-started).
   - What the tests assert, when the docs make a claim about behaviour (`tests/`).
3. **Compare doc by doc** and list each mismatch: the doc's claim, what the code does, and the file and line that shows it.
   Sort them:
   - **Doc is stale or incomplete**, the decision is unchanged: update the doc.
   - **A decision changed** (a different library, a protection layer dropped or weakened, a different data store, a
     changed rule): write a superseding ADR, then update the spec.
   - **Code is missing something the spec plans** for a later issue: leave the spec as the plan. Do not mark it done.
   - **Code contradicts the spec or a requirement with no decision behind it**: do not paper over it. Report it to the
     owner as a possible bug or an open decision.
4. **Edit the docs.**
   - Keep each change small and in the existing style: short sentences, plain words, requirement IDs (FR-n, AC-n.m,
     DEC-n) where they apply.
   - Describe behaviour, not code structure that changes often. Name a class or file only when a reader needs it.
   - Do not track issue or PR status in the spec beyond the delivery list; that belongs in GitHub.
   - Keep every relative link working. Check with:
     `grep -rhoE '\]\([^)#]+\.md' docs | sort -u` and confirm each target exists.
5. **Superseding an ADR.**
   - Copy the template from `docs/adr/README.md` into the next free number.
   - Context says what changed and why; Decision states the new decision; link the old record.
   - In the old record change only the status line to `Superseded by NNNN`. Leave its text as it was.
   - Update the index table: new row, old row's status.
   - Point the spec at the new record.
6. **Check the result.**
   - Re-read each changed section against the code once more.
   - No personal names, logins or emails anywhere (the repository is anonymous). Say "the project owner".
   - No secrets, keys or tokens, not even example values that look real.
7. **Report.** List each change by file, each superseding ADR, and each mismatch left open for the owner, with its
   evidence (file and line). Commit on the task branch with a message that names the task.

## Not this skill's job

- Changing code or tests.
- Rewriting requirements.md to match the implementation.
- Deciding an open question. Put it in front of the owner instead.
