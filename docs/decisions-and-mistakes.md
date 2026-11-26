# Decisions and mistakes

One line each: what went wrong, how it was caught, what changed.

- 2026-11-26: The first MVP plan dropped cross-site and DNS-rebinding protection although the endpoints spend money and write files. The owner caught it in plan review. Spec §5 added: loopback only, Host allowlist, API token, cross-site refusal, JSON-only bodies, GUID ids, export only under a configured root.
- 2026-11-26: The owner's answer "will review once the code is generated" about NuGet packages was read as approval to implement, and the whole MVP was pushed into the plan PR. The owner caught it. The plan PR now holds only the docs move and the spec, and no code is written until the owner approves the plan.
