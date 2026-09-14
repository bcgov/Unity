# SonarQube Compliance Guide

This file records SonarQube rules and coding patterns encountered in the CHEFS One-Click Form Tester. Update it whenever SonarQube reports a new error, warning, or advisory.

The goal is for all source code in this folder to remain fully compliant with SonarQube while preserving behavior through automated tests.

## Remediation Process

When SonarQube reports a finding:

1. Record the rule ID, message, triggering pattern, and preferred replacement in this file.
2. Inspect the exact reported line instead of inferring the issue from similar code.
3. Make the smallest behavior-preserving change.
4. Add or update a regression test when behavior could change.
5. Run syntax checks and the complete test suite.
6. Refresh SonarQube analysis and verify that the finding is gone.
7. Do not mark a finding resolved based only on changed line numbers; use a fresh report.

Useful validation commands from this directory:

```powershell
node --check content-script.js
node --check service-worker.js
node --test --test-reporter=spec
```

## Rules Encountered

### `javascript:S3776` — Cognitive Complexity

**Message:** Refactor this function to reduce its Cognitive Complexity.

Avoid large functions containing deeply nested conditionals, loops, retries, and unrelated responsibilities.

Preferred patterns:

- Extract focused helper functions with descriptive names.
- Use guard clauses and early returns to reduce nesting.
- Separate discovery, action, validation, retry, logging, and finalization logic.
- Move business decisions into helpers rather than merely moving blocks without reducing branching.
- Preserve operation order, retry counters, logging, and state transitions when decomposing workflows.

This rule drove decomposition of controller workflows for filling fields, uploads, grids, choices, submissions, validation recovery, and confirmation detection.

### `javascript:S7750` — Prefer `find()` over `filter()`

**Message:** Prefer `.find(...)` over `.filter(...)`.

When only one matching element is required, do not build an intermediate array.

Avoid:

```javascript
const match = values.filter((value) => predicate(value))[0];
```

Prefer:

```javascript
const match = values.find((value) => predicate(value));
```

Use `filter()` only when every matching element is needed.

### `javascript:S8786` — Regular Expression Super-Linear Runtime

**Message:** Simplify this regular expression to reduce its runtime, as it has super-linear performance due to backtracking.

Avoid regex structures that can repeatedly consume the same input in multiple ways, especially:

- Nested or overlapping quantifiers.
- Multiple broad optional groups next to one another.
- Unbounded `.*` or `\s*` sections around alternatives.
- Alternatives with overlapping prefixes.
- Repeated broad character classes followed by another broad matcher.

Preferred patterns:

- Normalize whitespace before matching so patterns can use literal spaces.
- Split one complex expression into sequential, focused checks.
- Bound searches by slicing a small relevant portion of input first.
- Use direct string operations when parsing delimiters or markup.
- Make alternatives mutually exclusive where practical.
- Use precise character classes and required delimiters.

Examples in this project include guidance parsing for numeric ranges, dates, checkbox limits, character limits, and word limits.

### `javascript:S5843` — Regular Expression Complexity

**Message:** Simplify this regular expression to reduce its complexity.

Even when runtime is bounded, avoid expressions with too many alternatives, groups, and combinations.

Preferred patterns:

- Break classification logic into ordered, named checks.
- Split a complex bidirectional match into two simple tests.
- Normalize input before matching.
- Use string methods such as `includes()`, `startsWith()`, `split()`, and `slice()` where clearer.
- Keep precedence explicit when separate patterns can match the same input.

Behavior-sensitive text classification should have regression tests for every precedence case.

### `javascript:S7760` / `javascript:S7780` — Use `String.raw`

**Message:** `String.raw` should be used to avoid escaping `\`.

Use `String.raw` for strings that represent regular-expression source or replacement text and contain significant backslashes.

Avoid heavily escaped dynamic regex source:

```javascript
const source = "\\b" + value + "\\s+";
```

Prefer:

```javascript
const source = String.raw`\b${value}\s+`;
```

Guidance:

- Regular-expression literals such as `/\s+/` do not need `String.raw`.
- Use it for string values passed to `RegExp`, or other strings where backslashes must remain literal.
- Verify interpolated fragments are trusted regex source or escaped appropriately.

### `javascript:S7757` — Prefer Class Field Declarations

**Message:** Prefer class field declaration over `this` assignment in the constructor for static values.

Declare stable default instance state as class fields instead of assigning every default in the constructor.

Avoid:

```javascript
class Controller {
  constructor() {
    this.running = false;
    this.items = new Map();
  }
}
```

Prefer:

```javascript
class Controller {
  running = false;
  items = new Map();
}
```

Important:

- Mutable values such as `Map`, `Set`, arrays, and objects must remain instance fields so each instance gets its own value.
- Keep constructor logic for actions that must execute during construction, such as registering event listeners.
- Add isolation tests when converting mutable constructor state to class fields.

### `javascript:S1854` — Useless Assignment

**Message:** Remove this useless assignment to a variable.

Do not initialize a variable when every path assigns it again before it is read.

Avoid:

```javascript
let role = '';
if (condition) {
  role = 'primary';
} else {
  role = 'secondary';
}
```

Prefer:

```javascript
let role;
if (condition) {
  role = 'primary';
} else {
  role = 'secondary';
}
```

Alternatively, return directly from branches or extract the selection into a helper when that is clearer.

### `javascript:S7766` — Prefer `Math.max()`

**Message:** Prefer `Math.max()` to simplify ternary expressions.

Use `Math.max()` when a conditional only clamps a numeric value to a lower bound.

Avoid:

```javascript
const minimum = value > 0 ? value : 0;
```

Prefer:

```javascript
const minimum = Math.max(0, value || 0);
```

Considerations:

- Preserve handling of `undefined`, `null`, zero, negative values, and `NaN`.
- Do not use `||` if a valid falsy value must remain distinguishable.
- Use `Math.min()` for equivalent upper-bound clamping.
- Add boundary tests when replacing conditional numeric normalization.

### `javascript:S7719` — Unnecessary `getTime()` Call

**Message:** Unnecessary `.getTime()` call.

The `Date` constructor accepts another `Date`, so an explicit timestamp conversion is unnecessary when cloning.

Avoid:

```javascript
const copy = new Date(source.getTime());
```

Prefer:

```javascript
const copy = new Date(source);
```

Use `getTime()` when a numeric timestamp is actually required, such as explicit comparisons or persisted numeric values. Tests should confirm copied dates do not mutate the source bounds.

### `javascript:S7758` — Prefer `String.fromCodePoint()`

**Message:** Prefer `String.fromCodePoint()` over `String.fromCharCode()`.

Prefer:

```javascript
const text = String.fromCodePoint(...values);
```

For byte-to-base64 conversion in this project, each value is in the range `0`–`255`, so `fromCodePoint()` preserves the existing behavior. Retain chunking to avoid exceeding argument limits and test all byte values plus chunk boundaries.

## Additional Patterns Identified During Remediation

These patterns were encountered while reducing the report, even when the final screenshot did not retain a visible rule ID.

### Duplicate String Literals

Extract repeated selectors, class prefixes, formatted values, and other stable literals into shared constants when repetition reduces maintainability.

Preferred approach:

- Use a named top-level constant for a repeated scalar.
- Group related selectors in an immutable object.
- Do not extract a value used only once merely to silence a metric.

### Boolean and Optional-Value Simplification

Use optional chaining, nullish coalescing, and direct boolean expressions when they preserve semantics and improve readability.

Verify the distinction between:

- `null` or `undefined` and other falsy values.
- Empty strings and missing values.
- Zero and absent numeric constraints.

### Collection Lookup and Transformation

Choose the collection operation that matches the intent:

- `find()` for one matching item.
- `some()` for existence.
- `every()` for universal checks.
- `filter()` only when all matches are required.
- `map()` only when producing a transformed collection.

Avoid creating temporary arrays solely to inspect one element or determine whether a match exists.

## Current Validation Baseline

At the time this guide was created:

- The report had been reduced from 217 findings to 10 reported findings.
- The final 10 reported findings covered `javascript:S7766`, `javascript:S7719`, and `javascript:S7758` and were remediated.
- `content-script.js` passed `node --check`.
- `service-worker.js` passed `node --check` after applying this guide to its initial 67 findings.
- The complete Node test run passed 108 tests with no failures.
- A refreshed SonarQube analysis is the authority for confirming zero remaining findings.

## New Rule Template

Add future findings using this format:

```markdown
### `javascript:RULE_ID` — Short Rule Name

**Message:** Exact SonarQube message.

**Triggering pattern:** Description of the reported code shape.

**Preferred pattern:** Description and minimal example of the compliant replacement.

**Behavioral considerations:** Edge cases and tests required to preserve behavior.
```
