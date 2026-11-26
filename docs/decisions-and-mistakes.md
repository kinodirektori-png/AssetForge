# Decisions and mistakes

One line each: what went wrong, how it was caught, what changed.

- 2026-11-26: The first MVP plan dropped cross-site and DNS-rebinding protection although the endpoints spend money and write files. The owner caught it in plan review. Spec §5 added: loopback only, Host allowlist, API token, cross-site refusal, JSON-only bodies, GUID ids, export only under a configured root.
- 2026-11-26: The owner's answer "will review once the code is generated" about NuGet packages was read as approval to implement, and the whole MVP was pushed into the plan PR. The owner caught it. The plan PR now holds only the docs move and the spec, and no code is written until the owner approves the plan.
- 2026-11-26: PR #15 put error text in the ProblemDetails title, treated every SQLite constraint failure as a duplicate, stored only CreatedAt as sortable text, and ordered a test by the real clock. The owner's review caught it. Titles are now fixed with the text in detail, only SQLITE_CONSTRAINT_UNIQUE (2067) is a conflict, all timestamps share one converter, and tests use a stepping fake clock.
