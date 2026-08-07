using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Unity.AI.Evaluation;

// Last-line defense against PII in committed fixtures. If any of these trip, the fixture is not
// clean and the PR fails. NOT clearance — a case that doesn't trip is still required to be
// synthetic-by-construction or hand-sanitized with reviewer sign-off.
internal static class SensitiveMarkers
{
    private static readonly (string Name, Regex Pattern)[] Patterns =
    [
        ("sin",              new Regex(@"\b\d{3}-?\d{3}-?\d{3}\b", RegexOptions.Compiled)),
        ("card_16_digit",    new Regex(@"\b(?:\d[ -]?){15}\d\b", RegexOptions.Compiled)),
        ("email",            new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled)),
        ("phone_na",         new Regex(@"\b(?:\+?1[\s.-]?)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}\b", RegexOptions.Compiled)),
    ];

    public static IReadOnlyList<string> Scan(string text)
    {
        var hits = new List<string>();
        if (string.IsNullOrEmpty(text))
        {
            return hits;
        }

        foreach (var (name, pattern) in Patterns)
        {
            if (pattern.IsMatch(text))
            {
                hits.Add(name);
            }
        }

        return hits;
    }
}
