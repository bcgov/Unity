---
globs: "**/modules/Unity.Flex/**, **/modules/Unity.TenantManagement/**, **/modules/Unity.Reporting/**, **/modules/Unity.SharedKernel/PostTenantCreation/**, **/Unity.GrantManager.Application/TenantManagement/**, **/Unity.GrantManager.Application/Tenants/PostCreation/**, **/Unity.GrantManager.Application/Reporting/**, **/Unity.GrantManager.Application/ApplicantProfile/**, **/Unity.GrantManager.Application/Messaging/**, **/Unity.GrantManager.Domain/Messaging/**, **/Unity.GrantManager.Domain/Applications/OnboardingApplicationManager.cs"
---

# Documentation Freshness

> The file you are editing is covered by a document in `documentation/`. Keep that document true.

## The rule

Before finishing a change in one of these areas, open `documentation/README.md`, find the row matching the path you edited, and check the listed docs. Update anything your change made **false**.

The bar is *"is anything in this doc now wrong?"* — not *"should I write up what I did?"*. Most changes need no doc edit. The things that do:

- A renamed or moved class, interface, file, or project cited by path in a doc.
- A changed state machine, permission gate, validation step, or ordered sequence that a doc enumerates.
- An added, removed, or re-signatured app service method or HTTP endpoint listed in a doc's API table.
- A behaviour a doc explicitly describes ("X always fires from state Y", "the button is enabled only when Z").
- A roadmap item you actually fixed — remove it from the relevant `*-roadmap.md`.

## Guardrails

- **Do not create new documentation files** for areas that have none. `documentation/README.md` lists the deliberately undocumented modules; adding docs for them is its own ticket, not a side effect of an unrelated change.
- **Do not add changelog phrasing.** These docs describe how the system works and why — not what changed when. No "as of <date>", no "recently updated", no "this was previously X".
- **Verify every path and class name you write** actually resolves. Stale file paths are the most common failure mode in these docs.
- If you add a doc to a feature folder, add it to that folder's `README.md` reading order too.
- Doc updates belong in the **same commit** as the code change that made them necessary — a follow-up commit is a follow-up that does not happen.

## Where the docs live

`documentation/README.md` is the source-path → doc index. Feature folders (`flex/`, `tenant-management/`, `reporting/`, `applicant-portal/`) each have their own `README.md` with a reading order and a source-location map.
