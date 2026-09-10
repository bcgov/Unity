# Cypress Selector Contract

Keeps every selector used by the Cypress suite (`applications/Unity.AutoUI/cypress`) cross-checked against the actual application markup (`applications/Unity.GrantManager/src` + `modules`), so a Razor/C#/JS change that quietly removes or renames an `id`/`data-cy`/`data-testid` a test depends on gets caught before it breaks a run.

All commands below run from `applications/Unity.AutoUI`.

## What's in this directory

- **`registry.json`** — generated output, one entry per distinct selector string found in Cypress code. **This file is meant to be committed** on `main`; it's the baseline every branch's `npm run selectors:diff` compares itself against. Regenerate and commit it after selector-affecting changes land on `main`, or the baseline drifts stale.

## The two tools

| Command | What it does |
|---|---|
| `npm run selectors:report` | Full snapshot: re-scans everything, writes `registry.json`, prints a summary. No comparison to anything — just "here's the state of the world right now." |
| `npm run selectors:diff` | Compares the current working tree against `registry.json` as it exists on `origin/main`, and reports any selector whose status got *worse* (a regression this branch likely introduced). |
| `npm run selectors:fix` | Same as `selectors:diff`, plus a dry-run preview of the patch it would apply for any regression it can confidently fix. Writes nothing. |
| `npm run selectors:apply` | Same as `selectors:fix`, but actually writes the unambiguous fixes to disk. Never commits, never pushes, never touches an ambiguous case. |

Pass `-- --base <ref>` to any diff/fix/apply command to compare against something other than `origin/main`.

## How the full scan works (`scripts/selector-contract.mjs`)

1. Walks `cypressRoots` (from `selector-contract.config.json`) and parses every `.ts`/`.tsx`/`.js`/`.jsx` file with the TypeScript compiler API, pulling out every string literal passed as the first argument to a selector-shaped call (`cy.get`, `.find`, `.contains`, `.xpath`, etc.) that looks like a selector.
2. Walks `applicationRoots` for `.cshtml`/`.razor`/`.html`/`.cs`/`.js`/`.ts` source.
3. For each selector, extracts identifying tokens (`#id`, `[data-cy=...]`, `[data-testid=...]`) and checks whether that literal token appears anywhere in the app source (`id="..."`, `asp-for="@Model...."` with underscores mapped to dots, etc.).
4. Classifies each selector:
   - **status**: `matched` (evidence found) / `missing` (identifying token, no evidence) / `unverified` (no identifying token to check — a structural CSS selector) / `exempt` (matches an `ownershipRules` pattern in `selector-contract.config.json`: identity-provider markup, framework-generated selectors like Bootstrap-select/Select2/DataTables/SweetAlert2, or Form.io/CHEFS dynamic fields).
   - **syntaxKind**: `css` or `xpath`. Nothing in this repo uses XPath today — this exists to flag it immediately if it ever shows up, since XPath selectors are harder to keep in sync with markup than `id`/`data-cy`.

`matched` proves the token exists somewhere in source. It does **not** prove the element is visible, enabled, permission-gated correctly, or on the route the test expects — it's a fast static check, not a substitute for running the spec.

## How the diff/fix works (`scripts/selector-diff-report.mjs`)

No git-diff parsing, no guessing at what changed. It just runs the same full scan against the current tree, fetches the baseline `registry.json` via `git show <base>:.../registry.json`, and compares `status` per selector by exact string match.

- **Regression** = a selector's status is worse now than at baseline. Only `matched → missing` regressions are eligible for auto-fix — that's the one case with concrete proof the selector used to work.
- **Fix candidate search**: for each missing token, look up which file(s) had it at baseline (`applicationMatches` in the baseline entry), pull the exact old line via `git show`, then score every line in the *current* version of that file by word-overlap similarity to the old line (with a small bonus for being near the original line number — renames overwhelmingly stay in place). One clear winner → propose restoring the attribute there. Zero or multiple close-scoring candidates → reported as "NEEDS REVIEW," nothing is touched.
- **`--apply`** only ever writes the unambiguous fixes. It inserts the missing attribute next to whatever's already on that element — it never removes or rewrites existing attributes, never invents a token value without baseline evidence, and never auto-commits.

Treat an applied fix like any other code change: read the diff, and run the actual Cypress spec that uses the selector before trusting it — restoring the token proves the selector *resolves* again, not that the underlying behavior is correct.

## As a Claude Code skill

`applications/Unity.AutoUI/.claude/skills/validate-cypress-selectors/SKILL.md` documents this same system for on-demand use inside a Claude Code session — invoke it by name when working on Cypress selectors, page objects, or Razor/C#/JS markup that backs them.

## Configuration

- **`selector-contract.config.json`** (repo root of `Unity.AutoUI`) — `cypressRoots`, `applicationRoots`, and `ownershipRules` (regex patterns + reason strings for what's exempt from the static contract).

## Files that need to be committed for this to work for everyone

| File | Why |
|---|---|
| `scripts/selector-contract.mjs` | The scanner/classifier — also exports the functions `selector-diff-report.mjs` reuses. |
| `scripts/selector-diff-report.mjs` | The baseline-diff + fix logic. |
| `selector-contract.config.json` | Scan roots and ownership exemptions. |
| `package.json` | `selectors:report` / `selectors:diff` / `selectors:fix` / `selectors:apply` npm scripts. |
| `.claude/skills/validate-cypress-selectors/SKILL.md` | The on-demand skill definition. |
| `cypress/selectors/registry.json` | **The baseline itself.** Without this committed on `main`, `selectors:diff`/`fix`/`apply` have nothing to compare against and will just print a "no baseline found" message. |
| `cypress/selectors/README.md` | This file. |
