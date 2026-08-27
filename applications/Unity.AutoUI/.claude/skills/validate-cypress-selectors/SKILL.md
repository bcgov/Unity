---
name: validate-cypress-selectors
description: Extract and audit Cypress selectors against Unity Grant Manager application markup, and detect when a branch's app-code changes have broken a selector that worked on main. Use when changing Razor/C#/JS markup, Cypress specs, page objects, shared Cypress commands, element IDs, or CSS selectors — or when investigating why a Cypress test started failing after unrelated app changes.
---

# Validate Cypress Selectors

Maintain the selector contract between `applications/Unity.AutoUI` and `applications/Unity.GrantManager`. Run everything from `applications/Unity.AutoUI`.

## Two modes

**Full snapshot** — classify every selector in the repo right now:
```bash
npm run selectors:report
```
Writes `cypress/selectors/registry.json`. Use this to get a baseline understanding, or before/after a change to eyeball the raw counts.

**Baseline diff (the useful one for "did this branch break something")**:
```bash
npm run selectors:diff              # report only
npm run selectors:fix               # + dry-run patch preview for fixable regressions
npm run selectors:apply             # + write the unambiguous fixes to disk
```
Compares the current working tree against the **committed baseline** — `cypress/selectors/registry.json` as it exists at `origin/main` (override with `--base <ref>`, e.g. `npm run selectors:diff -- --base origin/develop`). It does **not** re-parse git diffs to guess what changed; it re-runs the full scanner on the current tree and compares the resulting per-selector `status` against the baseline's. Anything that got *worse* (`matched` → `missing`, `matched` → `unverified`, etc.) is a regression this branch likely introduced.

## Interpreting a full-scan entry

- `matched` — every identifying token (`id`, `data-cy`, `data-testid`) has static evidence in application source.
- `missing` — at least one identifying token has no static evidence. Investigate.
- `unverified` — a structural/class-based CSS selector that can't be proven reliably by static scanning (no identifying token to search for).
- `exempt` — matches a configured ownership rule (`selector-contract.config.json`): external identity-provider markup, framework-generated selectors (Bootstrap-select, Select2, DataTables, SweetAlert2), or Form.io/CHEFS dynamic fields. Outside the static contract by design.
- `syntaxKind` — `css` or `xpath`. Everything today is `css`; `xpath` exists to catch it early if `cy.xpath(...)`-style selectors are ever introduced (they're harder to keep in sync with markup and generally discouraged).

`matched` only proves the token exists *somewhere* in source — it says nothing about visibility, permission-gating, or runtime reachability. It's a fast static sanity check, not a substitute for actually running the Cypress spec.

## Diff-mode regression auto-fix — how it decides what's safe to touch

Only `matched` (baseline) → `missing` (current) regressions are eligible for auto-fix — that's the one transition backed by concrete proof the selector worked before. Pre-existing `missing`/`unverified` entries are left alone; they aren't this branch's fault and guessing at them is out of scope.

For each eligible regression:
1. Find where the token was matched at baseline (`applicationMatches` in the baseline entry) and the exact line via `git show <base>:<file>`.
2. In the *current* version of that file, score every line for similarity to the old line (word-overlap, with a small proximity bonus toward the original line number — an attribute rename overwhelmingly stays in place rather than the element relocating).
3. **Exactly one clear winner** → propose restoring the missing attribute onto it (inserted right after the tag name, alongside whatever else is there — never replaces existing attributes).
4. **Zero or ambiguous candidates** (e.g. several structurally-identical sibling elements) → reported as "NEEDS REVIEW" with the old-baseline context shown. Never guessed.

`--apply` only ever writes the unambiguous fixes from step 3. It never touches an ambiguous case, never invents a token value it doesn't have baseline evidence for, and never commits or pushes — it stops at "working tree modified, go review and commit like any other change." Treat an applied fix the same as any other diff: read it, and actually run the relevant Cypress spec before trusting it, since restoring the identifying token proves the selector *resolves* again, not that the element behaves correctly.

## Workflow

1. Before editing UI selectors or markup: `npm run selectors:report` to capture a baseline mentally (or diff against origin/main if you want the machine to do it).
2. Prefer a unique `data-cy` attribute for application-owned interactive elements over relying on a plain `#id`.
3. Update application markup and the corresponding Cypress selector together.
4. `npm run selectors:diff` to see what changed relative to `origin/main`. If something regressed, `npm run selectors:fix` to preview a proposed patch, then `npm run selectors:apply` if it looks right — followed by actually running the affected Cypress spec.
5. For anything reported "NEEDS REVIEW," check the rendered page or Cypress scenario by hand — conditional, permission-gated, or JavaScript-generated elements won't resolve automatically.
6. Report unresolved findings; don't hide them by widening `ownershipRules` exemptions in `selector-contract.config.json` just to make a `missing` entry disappear.

## Keeping the baseline current

`cypress/selectors/registry.json` is a committed file that's only meaningful if `origin/main`'s copy is kept up to date — regenerate and commit it on `main` after selector-affecting changes land, otherwise `selectors:diff` will compare against a stale snapshot.
