using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Unity.AI.Evaluation;

internal static class DatasetHasher
{
    // Hash the dataset content deterministically: cases.jsonl + every fixture
    // file, sorted by relative path. Any change to a case or fixture flips
    // the hash — baseline comparison fails hard on mismatch.
    public static string Compute()
    {
        var root = DatasetLoader.DatasetRoot;
        if (!Directory.Exists(root))
        {
            return "sha256:empty";
        }

        using var sha = SHA256.Create();
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(f => !f.Replace('\\', '/').Contains("/private/"))
            .Select(f => (Rel: Path.GetRelativePath(root, f).Replace('\\', '/'), Full: f))
            .OrderBy(t => t.Rel, StringComparer.Ordinal)
            .ToList();

        var buf = new StringBuilder();
        foreach (var (rel, full) in files)
        {
            var bytes = File.ReadAllBytes(full);
            var fileHash = Convert.ToHexString(sha.ComputeHash(bytes));
            buf.Append(rel).Append(':').Append(fileHash).Append('\n');
        }

        var attachmentCases = DatasetLoader.LoadCsvCases(skipMissingAttachments: false)
            .Where(evalCase => !string.IsNullOrWhiteSpace(evalCase.AttachmentAbsolutePath)
                               && File.Exists(evalCase.AttachmentAbsolutePath))
            .OrderBy(evalCase => evalCase.Id, StringComparer.Ordinal)
            .ToList();
        foreach (var evalCase in attachmentCases)
        {
            var bytes = File.ReadAllBytes(evalCase.AttachmentAbsolutePath!);
            var fileHash = Convert.ToHexString(sha.ComputeHash(bytes));
            buf.Append("attachment/")
                .Append(evalCase.Id)
                .Append(':')
                .Append(fileHash)
                .Append('\n');
        }

        var final = sha.ComputeHash(Encoding.UTF8.GetBytes(buf.ToString()));
        return "sha256:" + Convert.ToHexString(final).ToLowerInvariant();
    }
}
