# Asset Forge

AI agent service that generates consistent 2D game art from a per-project style guide.
Read docs/spec.md before any task. The spec is the source of truth.

## How to work
- Before coding: restate the task, list the acceptance criteria you will satisfy,
  and name the files you plan to change. Wait for my OK if the plan touches more than 5 files.
- Keep changes small and reviewable. Prefer the simplest design that meets the spec.
  Do not add features, abstractions, or packages the spec does not ask for.
- If the spec is unclear or contradicts itself, ask me. Do not guess.

## Testing
- Every endpoint and rule gets tests for the happy path, edge cases, and failure scenarios.
- Code coverige with unit test should be least 80%.
- Write tests from the acceptance criteria in the spec, not from your own implementation.
- Run `dotnet build` and `dotnet test` before saying a task is done, and show the results.
- Never delete, skip, or weaken a test to make it pass. Tell me instead.

## Architecture rules
- External calls (image model, blob, chat model) go behind interfaces, mock implementations are the default in tests.
- Errors use ProblemDetails. Validate all input. Use EF Core only (no string-built SQL).
- Never log full prompts, keys, or tokens. Return safe error messages, no stack traces.

## Security and responsible AI
- Never commit secrets, keys, or connection strings. Use user-secrets locally and
  managed identity / Key Vault in Azure. Do not read or print .env or secrets files.
- Ask before adding any NuGet package: name it, say why, and note its licence.
- Respect content filter results: a Blocked generation is never retried.

## Git
- Work on a branch per task, never commit to main.
- Small commits with clear messages, without credentials. Include in a message wich task it solves. 
- Ask before any destructive or hard-to-reverse action (deleting files, dropping data,
  running migrations against anything but the local DB).

## Logging my learning
- When I correct you, or you find your own mistake, add one line to docs/decisions-and-mistakes.md:
  what went wrong, how it was caught, what changed.