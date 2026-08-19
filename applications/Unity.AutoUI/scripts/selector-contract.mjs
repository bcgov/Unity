#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { execFileSync } from "node:child_process";
import ts from "typescript";

export const autoUiRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
export const repoRoot = path.resolve(autoUiRoot, "../..");
export const configPath = path.join(autoUiRoot, "selector-contract.config.json");
export const registryPath = path.join(autoUiRoot, "cypress/selectors/registry.json");
const excludedDirectories = new Set(["node_modules", "bin", "obj", ".git", "coverage"]);

// XPath expressions start with a path axis (`/`, `//`, `./`) or use named
// axis syntax (`ancestor::`, `following-sibling::`, ...) or the `contains()`
// function form — none of which are valid CSS selector syntax. Selectors
// passed to `cy.xpath(...)` (the cypress-xpath plugin command) are also
// treated as XPath regardless of their text shape.
const XPATH_PATTERN =
  /^\.{0,2}\/\/|^\/[a-zA-Z*@]|::(?:ancestor|descendant|following-sibling|preceding-sibling|parent|self|child)(?:-or-self)?\b|contains\(\s*(?:text\(\)|@)/;

export function loadConfig() {
  return JSON.parse(fs.readFileSync(configPath, "utf8"));
}

export function walk(root, extensions) {
  const results = [];
  if (!fs.existsSync(root)) return results;
  for (const entry of fs.readdirSync(root, { withFileTypes: true })) {
    const fullPath = path.join(root, entry.name);
    if (entry.isDirectory()) {
      if (!excludedDirectories.has(entry.name) && !fullPath.includes(`${path.sep}wwwroot${path.sep}libs${path.sep}`)) {
        results.push(...walk(fullPath, extensions));
      }
    } else if (extensions.has(path.extname(entry.name).toLowerCase())) {
      results.push(fullPath);
    }
  }
  return results;
}

// Only scan files git actually tracks — a raw filesystem walk would also
// pick up local scratch/untracked files (e.g. a spec someone is drafting
// but never committed), which would leak into usedBy/applicationMatches in
// the committed registry and be internally inconsistent for anyone who
// doesn't have those same untracked files sitting on disk. Falls back to
// the plain filesystem walk if git isn't available at all (e.g. a tarball
// checkout with no .git directory).
export function trackedFiles(root, extensions) {
  const relRoot = path.relative(repoRoot, root);
  try {
    const output = execFileSync("git", ["ls-files", "--", relRoot], {
      cwd: repoRoot,
      encoding: "utf8",
      stdio: ["ignore", "pipe", "ignore"],
    });
    return output
      .split("\n")
      .filter(Boolean)
      .map((tracked) => path.join(repoRoot, tracked))
      .filter((fullPath) => extensions.has(path.extname(fullPath).toLowerCase()))
      .filter((fullPath) => !fullPath.includes(`${path.sep}wwwroot${path.sep}libs${path.sep}`))
      .sort();
  } catch {
    return walk(root, extensions);
  }
}

function selectorText(node, sourceFile) {
  if (ts.isStringLiteralLike(node) || ts.isNoSubstitutionTemplateLiteral(node)) return node.text.trim();
  if (ts.isTemplateExpression(node)) return node.getText(sourceFile).slice(1, -1).trim();
  return null;
}

function looksLikeSelector(value) {
  if (!value || value.length > 500 || /^(https?:|\/[^\s]+|\{.*\})/.test(value)) return false;
  if (XPATH_PATTERN.test(value)) return true;
  if (/^[#.\[]/.test(value)) return true;
  if (/^(html|body|main|nav|form|label|input|select|option|button|a|table|thead|tbody|tr|td|th|div|span|h[1-6])(?:$|[.#[:\s>+~,])/.test(value)) return true;
  return /^[a-z][a-z0-9-]*(?:\[[^\]]+\]|[#.][A-Za-z_-])/.test(value);
}

function isSelectorPosition(node) {
  const parent = node.parent;
  if (ts.isPropertyAssignment(parent) || ts.isPropertyDeclaration(parent) || ts.isVariableDeclaration(parent)) return true;
  if (!ts.isCallExpression(parent)) return false;
  const expression = parent.expression.getText();
  return parent.arguments.indexOf(node) === 0 && /(?:^|\.)(?:get|find|contains|within|closest|filter|children|parents|next|select|getElement|getBySelector|xpath)$/.test(expression);
}

function callName(node) {
  const parent = node.parent;
  if (!ts.isCallExpression(parent)) return null;
  return parent.expression.getText();
}

export function syntaxKind(selector, usedViaXpathCall) {
  if (usedViaXpathCall || XPATH_PATTERN.test(selector)) return "xpath";
  return "css";
}

export function extractSelectors(config) {
  const files = config.cypressRoots
    .map((root) => path.join(repoRoot, root))
    .flatMap((root) => trackedFiles(root, new Set([".ts", ".tsx", ".js", ".jsx"])))
    .sort();
  const selectors = new Map();
  for (const file of files) {
    const sourceFile = ts.createSourceFile(file, fs.readFileSync(file, "utf8"), ts.ScriptTarget.Latest, true);
    function visit(node) {
      const value = selectorText(node, sourceFile);
      if (value && isSelectorPosition(node) && looksLikeSelector(value)) {
        const position = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile));
        const usage = `${path.relative(repoRoot, file).replaceAll(path.sep, "/")}:${position.line + 1}`;
        const viaXpathCall = /(?:^|\.)xpath$/.test(callName(node) ?? "");
        if (!selectors.has(value)) {
          selectors.set(value, { usages: new Set(), viaXpathCall: false });
        }
        const entry = selectors.get(value);
        entry.usages.add(usage);
        entry.viaXpathCall = entry.viaXpathCall || viaXpathCall;
      }
      ts.forEachChild(node, visit);
    }
    visit(sourceFile);
  }
  return selectors;
}

function classifyOwnership(selector, ownershipRules) {
  const override = ownershipRules.find((rule) => rule.regex.test(selector));
  if (override) return override.ownership;
  if (selector.includes("${")) return "dynamic";
  if (/\[data-(?:cy|testid)=/.test(selector) || /(^|[\s>+~,])#[A-Za-z_]/.test(selector)) return "application";
  return "unclassified";
}

export function identifyingTokens(selector) {
  const tokens = [];
  for (const match of selector.matchAll(/#([A-Za-z_][\w:-]*)/g)) tokens.push({ type: "id", value: match[1] });
  for (const match of selector.matchAll(/\[(data-(?:cy|testid))=["']?([^\]"']+)/g)) tokens.push({ type: match[1], value: match[2] });
  return tokens;
}

// Shared with selector-diff-report.mjs so the "does this token exist in this
// source file" check and the "restore this token to this line" logic use
// the exact same string patterns validate() checks — a fix that satisfies
// buildNeedles() is guaranteed to flip the selector back to "matched".
export function buildNeedles(token) {
  const razorModelPath = token.value.replaceAll("_", ".");
  return token.type === "id"
    ? [
        `id=\"${token.value}\"`,
        `id='${token.value}'`,
        `Id(\"${token.value}\")`,
        `#${token.value}`,
        `'${token.value}'`,
        `\"${token.value}\"`,
        `asp-for=\"@Model.${razorModelPath}\"`,
        `asp-for='@Model.${razorModelPath}'`
      ]
    : [`${token.type}=\"${token.value}\"`, `${token.type}='${token.value}'`];
}

export function applicationIndex(config) {
  return config.applicationRoots
    .map((root) => path.join(repoRoot, root))
    .flatMap((root) => trackedFiles(root, new Set([".cshtml", ".razor", ".html", ".js", ".ts", ".tsx", ".cs"])))
    .sort()
    .map((file) => ({ file: path.relative(repoRoot, file).replaceAll(path.sep, "/"), text: fs.readFileSync(file, "utf8") }));
}

/**
 * @param {Map<string, {usages: Set<string>, viaXpathCall: boolean}>} selectors
 */
export function validate(selectors, config, sources) {
  const ownershipRules = config.ownershipRules.map((rule) => ({ ...rule, regex: new RegExp(rule.pattern) }));
  return [...selectors.entries()].sort(([a], [b]) => a.localeCompare(b)).map(([selector, { usages, viaXpathCall }]) => {
    const ownership = classifyOwnership(selector, ownershipRules);
    const tokens = identifyingTokens(selector);
    const matches = new Set();
    const missingTokens = [];
    for (const token of tokens) {
      const needles = buildNeedles(token);
      const tokenMatches = sources.filter((source) => needles.some((needle) => source.text.includes(needle)));
      if (tokenMatches.length === 0) missingTokens.push(`${token.type}:${token.value}`);
      tokenMatches.slice(0, 20).forEach((source) => matches.add(source.file));
    }
    let status = "unverified";
    if (["external", "framework-generated", "dynamic"].includes(ownership)) status = "exempt";
    else if (tokens.length > 0) status = missingTokens.length === 0 ? "matched" : "missing";
    return {
      selector,
      kind: selector.includes("${") ? "dynamic" : tokens[0]?.type ?? "css",
      syntaxKind: syntaxKind(selector, viaXpathCall),
      ownership,
      status,
      missingTokens,
      usedBy: [...usages].sort(),
      applicationMatches: [...matches].sort()
    };
  });
}

export function buildRegistry(config = loadConfig()) {
  const entries = validate(extractSelectors(config), config, applicationIndex(config));
  const counts = Object.fromEntries(["matched", "missing", "unverified", "exempt"].map((status) => [status, entries.filter((entry) => entry.status === status).length]));
  const syntaxCounts = Object.fromEntries(["css", "xpath"].map((k) => [k, entries.filter((entry) => entry.syntaxKind === k).length]));
  return {
    schemaVersion: 2,
    mode: "report-only",
    generatedAt: new Date().toISOString(),
    summary: { total: entries.length, ...counts, syntax: syntaxCounts },
    entries
  };
}

function runCli() {
  const config = loadConfig();
  const registry = buildRegistry(config);
  fs.mkdirSync(path.dirname(registryPath), { recursive: true });
  fs.writeFileSync(registryPath, `${JSON.stringify(registry, null, 2)}\n`);
  console.log(`Cypress selector contract: ${registry.summary.total} selectors`);
  for (const status of ["matched", "missing", "unverified", "exempt"]) console.log(`  ${status}: ${registry.summary[status]}`);
  console.log(`  syntax — css: ${registry.summary.syntax.css}, xpath: ${registry.summary.syntax.xpath}`);
  for (const entry of registry.entries.filter((candidate) => candidate.status === "missing")) console.log(`  MISSING ${entry.selector} (${entry.usedBy.join(", ")})`);
  for (const entry of registry.entries.filter((candidate) => candidate.syntaxKind === "xpath")) console.log(`  XPATH ${entry.selector} (${entry.usedBy.join(", ")})`);
  console.log(`Registry written to ${path.relative(repoRoot, registryPath)}`);
}

// Only run the CLI report when this file is executed directly
// (`node scripts/selector-contract.mjs`) — not when imported as a module by
// selector-diff-report.mjs.
if (import.meta.url === `file://${process.argv[1]}`) {
  runCli();
}
