"""
Whitespace-only SQL pretty-printer used to make `gold_sql` reviewable in the
spreadsheet templates. Pure offline transform, no network calls, no deps.

The one hard rule: `collapse_sql(format_sql(s)) == s` for any canonical `s`.
Formatting only ever *replaces an existing space with a newline + indent* — it
never inserts, drops, reorders, re-cases or rewrites a single character of SQL.
That is what lets a spreadsheet/CSV pretty-print gold_sql for review while
csv_to_jsonl.py collapses it straight back to the stored one-liner, so a CSV
round-trip with no SME edits leaves questions.jsonl byte-identical (and never
falsely trips the "gold_sql changed -> re-capture" path in csv_to_jsonl.py).

Style: a query that fits on one line stays on one line. Anything longer breaks
before each clause keyword, and a clause that still doesn't fit puts its list
items / ON / AND / OR on their own lines. Parens that aren't subqueries
(function args, OVER (...), IN (...), FILTER (...)) are always left inline.
"""
import re

MAX_WIDTH = 74
INDENT = "  "

# Words that start a new clause line, at the indent of their paren depth.
CLAUSE_KEYWORDS = {
    "SELECT", "FROM", "WHERE", "HAVING", "WINDOW", "LIMIT", "OFFSET", "FETCH",
    "UNION", "INTERSECT", "EXCEPT", "WITH", "JOIN", "NATURAL",
}
# Need a following "BY" to count as a clause (vs. an unrelated GROUP/ORDER word).
CLAUSE_KEYWORDS_WITH_BY = {"GROUP", "ORDER"}
# Need a following JOIN/OUTER to count as a clause (vs. the LEFT() function).
JOIN_PREFIXES = {"LEFT", "RIGHT", "INNER", "FULL", "CROSS"}
# Words that start a continuation line, indented one level past their clause.
ITEM_KEYWORDS = {"ON", "AND", "OR", "USING"}
# A "(" directly followed by one of these opens a subquery, which we format;
# any other "(" is a function call or value list and stays on one line.
SUBQUERY_STARTERS = {"SELECT", "WITH", "VALUES"}

_WORD_RE = re.compile(r"[^\s(),'\"]+")


class _Atom:
    __slots__ = ("text", "space_before", "depth", "word", "formattable")

    def __init__(self, text, space_before, word):
        self.text = text
        self.space_before = space_before
        self.word = word  # uppercased text for bare words, "" for quoted/punctuation
        self.depth = 0  # paren nesting depth, filled in by _mark_parens
        self.formattable = True  # False anywhere inside a non-subquery paren


def _end_of_quoted(sql: str, start: int) -> int:
    """Index just past the string/identifier literal opening at `start`, with
    a doubled quote read as an escape. Runs to the end if it's unterminated."""
    quote = sql[start]
    i, n = start + 1, len(sql)
    while i < n:
        if sql[i] != quote:
            i += 1
        elif i + 1 < n and sql[i + 1] == quote:
            i += 2
        else:
            return i + 1
    return n


def _split_atoms(sql: str) -> list:
    """Splits SQL into atoms. Quoted strings/identifiers are atomic, so a
    keyword or paren inside them is never mistaken for syntax."""
    atoms = []
    i, n = 0, len(sql)
    space_before = False

    while i < n:
        ch = sql[i]
        if ch.isspace():
            space_before = True
            i += 1
            continue

        if ch in ("'", '"'):
            end = _end_of_quoted(sql, i)
            atoms.append(_Atom(sql[i:end], space_before, ""))
            i = end
        elif ch in ("(", ")", ","):
            atoms.append(_Atom(ch, space_before, ""))
            i += 1
        else:
            text = _WORD_RE.match(sql, i).group(0)
            atoms.append(_Atom(text, space_before, text.upper()))
            i += len(text)

        space_before = False

    return atoms


def _mark_parens(atoms: list) -> None:
    """Assigns each atom its paren depth, and clears `formattable` on anything
    inside a paren that isn't a subquery."""
    stack = [True]
    for i, atom in enumerate(atoms):
        if atom.text == ")" and len(stack) > 1:
            stack.pop()
        atom.depth = len(stack) - 1
        atom.formattable = stack[-1]
        if atom.text == "(":
            nxt = atoms[i + 1].word if i + 1 < len(atoms) else ""
            stack.append(stack[-1] and nxt in SUBQUERY_STARTERS)


def _tokenize(sql: str) -> list:
    atoms = _split_atoms(sql)
    _mark_parens(atoms)
    return atoms


def _is_clause_start(atoms: list, i: int) -> bool:
    atom = atoms[i]
    if not atom.formattable or not atom.word:
        return False
    if atom.word in CLAUSE_KEYWORDS:
        return True
    nxt = atoms[i + 1].word if i + 1 < len(atoms) else ""
    if atom.word in CLAUSE_KEYWORDS_WITH_BY:
        return nxt == "BY"
    if atom.word in JOIN_PREFIXES:
        return nxt in ("JOIN", "OUTER")
    return False


def _segment_end(atoms: list, start: int) -> int:
    """Index just past the clause starting at `start`: the next clause keyword
    at the same depth, or wherever that depth closes."""
    depth = atoms[start].depth
    for i in range(start + 1, len(atoms)):
        if atoms[i].depth < depth:
            return i
        if atoms[i].depth == depth and _is_clause_start(atoms, i):
            return i
    return len(atoms)


def _inline_width(atoms: list, start: int, end: int) -> int:
    return sum(len(a.text) + (1 if a.space_before and i > start else 0)
               for i, a in enumerate(atoms[start:end], start))


class _BreakState:
    """What the emit loop has to remember between atoms."""

    __slots__ = ("inline_until", "open_clause_depths", "suppress_and")

    def __init__(self):
        self.inline_until = 0  # atoms up to here belong to a clause measured to fit
        # Depths that currently have an open clause. Continuation lines only
        # break at a depth that has one, so an AND belonging to the outer WHERE
        # indents against that WHERE even when a subquery just closed inline.
        self.open_clause_depths = set()
        self.suppress_and = False  # BETWEEN x AND y must not split on its AND


def _start_clause(atoms: list, i: int, state: _BreakState, max_width: int) -> int:
    """Opens the clause at `i` and returns the indent its line breaks to."""
    depth = atoms[i].depth
    state.open_clause_depths = {d for d in state.open_clause_depths if d <= depth}
    state.open_clause_depths.add(depth)
    end = _segment_end(atoms, i)
    if _inline_width(atoms, i, end) + len(INDENT) * depth <= max_width:
        state.inline_until = end  # whole clause fits — keep it on one line
    return depth


def _break_indent(atoms: list, i: int, state: _BreakState, max_width: int):
    """Indent level to break to before atoms[i], or None to keep it inline."""
    atom = atoms[i]
    if not atom.formattable or i < state.inline_until:
        return None
    if _is_clause_start(atoms, i):
        return _start_clause(atoms, i, state, max_width)
    if atom.depth not in state.open_clause_depths:
        return None
    continues_clause = atom.word in ITEM_KEYWORDS or atoms[i - 1].text == ","
    if not continues_clause or (atom.word == "AND" and state.suppress_and):
        return None
    return atom.depth + 1


def format_sql(sql: str, max_width: int = MAX_WIDTH) -> str:
    """Pretty-prints `sql` by replacing selected spaces with newlines. Returns
    it unchanged if it already fits on one line, or if the result would not
    collapse back to it exactly (a malformed query, say)."""
    sql = collapse_sql(sql)
    if len(sql) <= max_width:
        return sql

    atoms = _tokenize(sql)
    state = _BreakState()
    out = []

    for i, atom in enumerate(atoms):
        indent = _break_indent(atoms, i, state, max_width)
        if atom.word == "BETWEEN":
            state.suppress_and = True
        elif atom.word == "AND":
            state.suppress_and = False

        # A break has to consume an existing space, or it wouldn't collapse back.
        if not (atom.space_before and out):
            out.append(atom.text)
        elif indent is None:
            out.append(" " + atom.text)
        else:
            out.append("\n" + INDENT * indent + atom.text)

    formatted = "".join(out)
    return formatted if collapse_sql(formatted) == sql else sql


def collapse_sql(sql: str) -> str:
    """Inverse of format_sql: collapses every whitespace run outside of quoted
    strings/identifiers to a single space. This is the canonical form gold_sql
    is stored in inside questions.jsonl."""
    atoms = _split_atoms(sql)
    return "".join((" " if a.space_before and i else "") + a.text
                   for i, a in enumerate(atoms))


def has_comment(sql: str) -> bool:
    """True if `sql` contains a -- or /* comment outside a quoted literal.
    Collapsing such a query to one line would comment out everything after a
    `--`, so callers reject it instead of silently mangling the query."""
    return any("--" in a.word or "/*" in a.word for a in _split_atoms(sql))
