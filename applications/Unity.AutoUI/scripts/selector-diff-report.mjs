#!/usr/bin/env node

// Compares the Cypress selector contract on the current branch/working tree
// against a committed baseline (cypress/selectors/registry.json as it
// exists at --base, default origin/main) to find selectors that *worked at
// the baseline but no longer do* — i.e. this branch likely broke them.
//
// Modes:
//   node scripts/selector-diff-report.mjs            report only
//   node scripts/selector-diff-report.mjs --fix       report + dry-run patch preview
//   node scripts/selector-diff-report.mjs --apply     report + write unambiguous fixes to disk
//   node scripts/selector-diff-report.mjs --base <ref>  compare against a different ref
//
// Never commits or pushes. --apply only ever restores a token this script
// has concrete baseline evidence for, and only when exactly one unambiguous
// candidate line is found in the current file — anything else is left for a
// human to resolve and is reported as "needs review".

import fs from "node:fs";
import path from "node:path";
import { execFileSync } from "node:child_process";
import {
  repoRoot,
  registryPath,
  loadConfig,
  extractSelectors,
  applicationIndex,
  validate,
  identifyingTokens,
  buildNeedles
} from "./selector-contract.mjs";

const args = process.argv.slice(2);
const mode = args.includes("--apply") ? "apply" : args.includes("--fix") ? "fix" : "report";
const baseIndex = args.indexOf("--base");
const baseRef = baseIndex !== -1 ? args[baseIndex + 1] : "origin/main";

const STATUS_RANK = { missing: 0, unverified: 1, exempt: 2, matched: 3 };
const registryRelPath = path.relative(repoRoot, registryPath).replaceAll(path.sep, "/");

function git(argv) {
  return execFileSync("git", argv, { cwd: repoRoot, encoding: "utf8", stdio: ["ignore", "pipe", "ignore"] });
}

function readBaselineRegistry() {
  try {
    const raw = git(["show", `${baseRef}:${registryRelPath}`]);
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

function readBaselineFile(relPath) {
  try {
    return git(["show", `${baseRef}:${relPath}`]);
  } catch {
    return null;
  }
}

function findLineWithNeedle(text, needles) {
  const lines = text.split("\n");
  for (let i = 0; i < lines.length; i += 1) {
    if (needles.some((needle) => lines[i].includes(needle))) {
      return { lineNumber: i + 1, text: lines[i] };
    }
  }
  return null;
}

// Crude but dependency-free line-similarity score: fraction of shared
// "words" (tag names, attribute names/values) between two lines. Good
// enough to spot "this is clearly the same element, just missing the
// attribute" without pulling in a real HTML/Razor parser — consistent with
// how the rest of this tool already does plain-text scanning rather than
// AST-level analysis of the application source.
function similarity(lineA, lineB) {
  const wordsOf = (line) => new Set(line.toLowerCase().match(/[a-z0-9_-]+/g) ?? []);
  const a = wordsOf(lineA);
  const b = wordsOf(lineB);
  if (a.size === 0 || b.size === 0) return 0;
  let shared = 0;
  for (const word of a) if (b.has(word)) shared += 1;
  return shared / Math.max(a.size, b.size);
}

function findRestoreCandidate(oldLineText, oldLineNumber, currentFileText) {
  const currentLines = currentFileText.split("\n");
  const scored = currentLines
    .map((text, index) => {
      const lineNumber = index + 1;
      const score = similarity(oldLineText, text);
      // An attribute rename/removal overwhelmingly happens in place — the
      // element doesn't relocate elsewhere in the file. Use distance from
      // the original line as a tiebreaker so two structurally-similar
      // sibling lines (e.g. repeated `<abp-input asp-for="@Model.X.Y">`
      // rows) don't score as equally likely; it only ever nudges the
      // ranking, never overrides a genuinely higher word-overlap score.
      const proximityBonus = 1 / (1 + Math.abs(lineNumber - oldLineNumber));
      return { lineNumber, text, score, ranked: score + proximityBonus * 0.05 };
    })
    .filter((candidate) => candidate.score > 0)
    .sort((a, b) => b.ranked - a.ranked);

  if (scored.length === 0 || scored[0].score < 0.6) return { candidate: null, reason: "no line above the similarity threshold" };
  if (scored.length > 1 && scored[1].ranked >= scored[0].ranked - 0.02) {
    return { candidate: null, reason: `ambiguous — top two candidates score ${scored[0].score.toFixed(2)} (line ${scored[0].lineNumber}) and ${scored[1].score.toFixed(2)} (line ${scored[1].lineNumber})` };
  }
  return { candidate: scored[0], reason: null };
}

function proposeInsertion(lineText, token) {
  const attr = token.type === "id" ? "id" : token.type;
  const tagMatch = lineText.match(/<([a-zA-Z][a-zA-Z0-9-]*)/);
  if (!tagMatch) return null;
  const insertAt = tagMatch.index + tagMatch[0].length;
  return `${lineText.slice(0, insertAt)} ${attr}="${token.value}"${lineText.slice(insertAt)}`;
}

function main() {
  const config = loadConfig();
  const baseline = readBaselineRegistry();

  if (!baseline) {
    console.log(`No baseline registry found at ${baseRef}:${registryRelPath}.`);
    console.log("This is expected before cypress/selectors/registry.json has been committed on the base branch.");
    console.log("Run `npm run selectors:report` and commit the result on your base branch first, then re-run this.");
    return;
  }

  const currentEntries = validate(extractSelectors(config), config, applicationIndex(config));
  const baselineBySelector = new Map(baseline.entries.map((entry) => [entry.selector, entry]));

  const regressions = [];
  const newlyBroken = [];

  for (const current of currentEntries) {
    const before = baselineBySelector.get(current.selector);
    if (!before) {
      if (current.status === "missing") newlyBroken.push(current);
      continue;
    }
    if (STATUS_RANK[current.status] < STATUS_RANK[before.status]) {
      regressions.push({ before, current });
    }
  }

  console.log(`Selector contract diff — base: ${baseRef}`);
  console.log(`  ${currentEntries.length} selectors on current tree, ${baseline.entries.length} at baseline`);
  console.log(`  regressions: ${regressions.length}, new-and-already-broken: ${newlyBroken.length}`);
  console.log("");

  if (regressions.length === 0 && newlyBroken.length === 0) {
    console.log("No selector regressions found relative to the baseline.");
    return;
  }

  for (const { before, current } of regressions) {
    console.log(`REGRESSED  ${current.selector}`);
    console.log(`  status: ${before.status} -> ${current.status}`);
    console.log(`  used by: ${current.usedBy.join(", ")}`);

    if (before.status !== "matched" || current.status !== "missing") {
      console.log("  (not auto-fixable — only matched -> missing regressions are attempted)\n");
      continue;
    }

    const tokens = identifyingTokens(current.selector).filter((token) =>
      current.missingTokens.includes(`${token.type}:${token.value}`),
    );

    for (const token of tokens) {
      const needles = buildNeedles(token);
      let resolved = false;

      for (const relPath of before.applicationMatches) {
        const oldFileText = readBaselineFile(relPath);
        if (oldFileText === null) continue;
        const oldLine = findLineWithNeedle(oldFileText, needles);
        if (!oldLine) continue;

        const absPath = path.join(repoRoot, relPath);
        if (!fs.existsSync(absPath)) {
          console.log(`  ${token.type}:${token.value} — baseline evidence in ${relPath}:${oldLine.lineNumber}, but that file no longer exists. NEEDS REVIEW.`);
          resolved = true;
          break;
        }

        const currentFileText = fs.readFileSync(absPath, "utf8");
        const { candidate, reason } = findRestoreCandidate(oldLine.text, oldLine.lineNumber, currentFileText);

        if (!candidate) {
          console.log(`  ${token.type}:${token.value} — was in ${relPath}:${oldLine.lineNumber} (${oldLine.text.trim()}). NEEDS REVIEW: ${reason}.`);
          resolved = true;
          break;
        }

        const fixedLine = proposeInsertion(candidate.text, token);
        if (!fixedLine) {
          console.log(`  ${token.type}:${token.value} — matched ${relPath}:${candidate.lineNumber} but couldn't locate a tag to attach the attribute to. NEEDS REVIEW.`);
          resolved = true;
          break;
        }

        console.log(`  ${token.type}:${token.value} — proposed fix in ${relPath}:${candidate.lineNumber}`);
        console.log(`    - ${candidate.text.trim()}`);
        console.log(`    + ${fixedLine.trim()}`);

        if (mode === "apply") {
          const lines = currentFileText.split("\n");
          lines[candidate.lineNumber - 1] = fixedLine;
          fs.writeFileSync(absPath, lines.join("\n"));
          console.log(`    applied to ${relPath}`);
        }

        resolved = true;
        break;
      }

      if (!resolved) {
        console.log(`  ${token.type}:${token.value} — no baseline evidence found to restore from. NEEDS REVIEW.`);
      }
    }
    console.log("");
  }

  for (const entry of newlyBroken) {
    console.log(`NEW, ALREADY MISSING  ${entry.selector}`);
    console.log(`  used by: ${entry.usedBy.join(", ")}`);
    console.log(`  missing: ${entry.missingTokens.join(", ")}\n`);
  }

  if (mode === "report") {
    console.log("Run with --fix to preview proposed patches, or --apply to write unambiguous fixes to disk.");
  } else if (mode === "fix") {
    console.log("Dry run only — nothing was written. Re-run with --apply to write the unambiguous fixes above.");
  }
}

main();
