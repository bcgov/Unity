using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Unity.AI.Evaluation;

// Candidate summaries and judge explanations may repeat private attachment data.
// They are written only when an explicit directory is supplied, never under the
// normal reports directory that CI uploads.
internal static class PrivateAuditWriter
{
    public static string? WriteIfEnabled(EvalRun run)
    {
        var configuredDirectory = Environment.GetEnvironmentVariable("EVAL_PRIVATE_AUDIT_DIR");
        if (string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return null;
        }

        var auditDirectory = Path.GetFullPath(configuredDirectory);
        var reportsDirectory = Path.GetFullPath(DatasetLoader.ReportsRoot);
        if (IsSameOrChildPath(auditDirectory, reportsDirectory))
        {
            throw new InvalidOperationException(
                "EVAL_PRIVATE_AUDIT_DIR must be outside the normal reports directory because reports are uploaded by CI.");
        }

        Directory.CreateDirectory(auditDirectory);
        var path = Path.Combine(auditDirectory, $"audit-{run.UtcTimestamp}.private.json");
        var payload = new
        {
            warning = "PRIVATE: candidate summaries and judge evidence may contain attachment data; do not commit or upload.",
            run.UtcTimestamp,
            run.DatasetHash,
            run.CaseSetHash,
            cases = run.Cases.Select(result => new
            {
                result.CaseId,
                result.FileName,
                candidateSummary = result.CandidateSummary,
                judge = result.Judge.Audit,
            }),
        };
        File.WriteAllText(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
        }));
        return path;
    }

    private static bool IsSameOrChildPath(string candidate, string parent)
    {
        var relative = Path.GetRelativePath(parent, candidate);
        return relative == "."
            || (!relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !Path.IsPathRooted(relative));
    }
}
