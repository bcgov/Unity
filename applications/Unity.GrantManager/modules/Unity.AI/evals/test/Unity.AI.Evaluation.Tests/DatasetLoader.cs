using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Unity.AI.Evaluation;

internal static class DatasetLoader
{
    private static readonly string[] RequiredCsvHeaders =
    [
        "tenant",
        "attachment_id",
        "file_name",
        "chefs_submission_id",
        "chefs_file_id",
        "tags_json",
        "extraction_status",
        "extracted_text_length",
        "extracted_text_sha256",
        "baseline_summary",
        "expected_facts_json",
        "hallucination_traps_json",
    ];

    public static string DatasetRoot =>
        Path.Combine(AppContext.BaseDirectory, "datasets", "attachment-summary");

    public static string? ProjectRoot =>
        Environment.GetEnvironmentVariable("EVAL_PROJECT_DIR")
        ?? FindAncestorContaining("Unity.AI.Evaluation.Tests.csproj");

    public static string WritableDatasetRoot =>
        ProjectRoot is null
            ? DatasetRoot
            : Path.Combine(ProjectRoot, "datasets", "attachment-summary");

    public static string ReportsRoot =>
        Environment.GetEnvironmentVariable("EVAL_REPORTS_DIR")
        ?? (ProjectRoot is null
            ? Path.Combine(AppContext.BaseDirectory, "reports")
            : Path.Combine(ProjectRoot, "reports"));

    // CSV lives at modules/Unity.AI/evals/data/ and is linked into the test
    // output at datasets/attachment-summary/data/ (see csproj). Loading from
    // the copied location keeps DatasetHasher covering it.
    public static string CsvPath =>
        Path.Combine(DatasetRoot, "data", "attachment-summary-eval.csv");

    // Real binaries are never committed to test output. Discovered from env
    // var or by walking up from the test binary directory to the module tree.
    public static string AttachmentsRoot =>
        Environment.GetEnvironmentVariable("EVAL_ATTACHMENTS_DIR")
        ?? FindAttachmentsRoot()
        ?? "";

    public static IReadOnlyList<EvalCase> LoadCases(bool requireCsvAttachments = false)
    {
        var cases = new List<EvalCase>();
        cases.AddRange(LoadJsonlCases());
        cases.AddRange(LoadCsvCases(skipMissingAttachments: !requireCsvAttachments));

        if (requireCsvAttachments)
        {
            var missing = cases
                .Where(c => c.Source == "csv" &&
                            (string.IsNullOrWhiteSpace(c.AttachmentAbsolutePath) ||
                             !File.Exists(c.AttachmentAbsolutePath)))
                .Select(c => c.Id)
                .ToList();
            if (missing.Count > 0)
            {
                throw new FileNotFoundException(
                    $"Missing {missing.Count} evaluation attachment(s) under '{AttachmentsRoot}': " +
                    string.Join(", ", missing));
            }
        }

        return cases;
    }

    public static IReadOnlyList<EvalCase> LoadJsonlCases()
    {
        var casesFile = Path.Combine(DatasetRoot, "cases.jsonl");
        if (!File.Exists(casesFile))
        {
            return Array.Empty<EvalCase>();
        }

        var cases = new List<EvalCase>();
        foreach (var line in File.ReadAllLines(casesFile))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//"))
            {
                continue;
            }

            var evalCase = JsonSerializer.Deserialize<EvalCase>(trimmed, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
            if (evalCase != null)
            {
                evalCase.Source = "jsonl";
                cases.Add(evalCase);
            }
        }

        return cases;
    }

    // Reads the tenant-facing CSV as EvalCases and resolves each row to the
    // matching downloaded attachment. Rows without a file on disk are skipped
    // when skipMissingAttachments=true; the offline dataset validator uses
    // false so it can report absence explicitly.
    public static IReadOnlyList<EvalCase> LoadCsvCases(bool skipMissingAttachments)
    {
        if (!File.Exists(CsvPath))
        {
            return Array.Empty<EvalCase>();
        }

        var attachmentsById = IndexAttachments(AttachmentsRoot);
        var rows = ReadCsv(CsvPath);
        if (rows.Count == 0)
        {
            return Array.Empty<EvalCase>();
        }

        var header = rows[0];
        var idx = BuildHeaderIndex(header);
        ValidateRequiredHeaders(idx);
        var cases = new List<EvalCase>(rows.Count - 1);

        for (var r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            if (row.Count == 1 && string.IsNullOrWhiteSpace(row[0]))
            {
                continue;
            }

            if (row.Count != header.Count)
            {
                throw new InvalidDataException(
                    $"CSV row {r + 1} has {row.Count} columns; expected {header.Count}.");
            }

            var tenant = Field(row, idx, "tenant");
            var attachmentId = Field(row, idx, "attachment_id");
            var fileName = Field(row, idx, "file_name");
            if (string.IsNullOrWhiteSpace(attachmentId) || string.IsNullOrWhiteSpace(fileName))
            {
                throw new InvalidDataException(
                    $"CSV row {r + 1} must define attachment_id and file_name.");
            }

            attachmentsById.TryGetValue(attachmentId, out var absolute);
            if (skipMissingAttachments && (string.IsNullOrEmpty(absolute) || !File.Exists(absolute)))
            {
                continue;
            }

            var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
            var structuredTags = ParseJsonArray<string>(
                Field(row, idx, "tags_json"),
                r + 1,
                "tags_json");
            var extractionStatus = Field(row, idx, "extraction_status");
            var extractedTextLength = ParseNonNegativeInt(
                Field(row, idx, "extracted_text_length"),
                r + 1,
                "extracted_text_length");
            var extractedTextSha256 = Field(row, idx, "extracted_text_sha256");
            var baselineSummary = Field(row, idx, "baseline_summary");
            var facts = ParseJsonArray<EvalFact>(
                Field(row, idx, "expected_facts_json"),
                r + 1,
                "expected_facts_json");
            var traps = ParseJsonArray<EvalHallucinationTrap>(
                Field(row, idx, "hallucination_traps_json"),
                r + 1,
                "hallucination_traps_json");
            var documentType = TagValue(structuredTags, "document_type:");
            var documentState = TagValue(structuredTags, "document_state:");
            var difficulty = TagValue(structuredTags, "difficulty:");
            var trapTypes = traps
                .Select(trap => trap.Type)
                .Where(type => !string.IsNullOrWhiteSpace(type))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var tags = new List<string>();
            AddIfNonEmpty(tags, "csv");
            AddIfNonEmpty(tags, tenant);
            AddIfNonEmpty(tags, extension);
            AddIfNonEmpty(tags, extractionStatus);
            foreach (var structuredTag in structuredTags)
            {
                AddIfNonEmpty(tags, structuredTag);
            }

            cases.Add(new EvalCase
            {
                Id = attachmentId,
                Tags = tags,
                FileName = fileName,
                ContentType = ContentTypeForExtension(extension, fileName),
                FixturePath = null,
                ExtractedText = null,
                ExtractedTextPath = null,
                PromptVersion = null,
                ExpectedFacts = facts.Select(fact => fact.Text).ToList(),
                ForbiddenClaims = traps.Select(trap => trap.ForbiddenClaim).ToList(),
                ReferenceSummary = string.IsNullOrWhiteSpace(baselineSummary) ? null : baselineSummary,
                ReviewerNotes = null,
                DocumentType = documentType,
                DocumentState = documentState,
                Difficulty = difficulty,
                TrapTypes = trapTypes,
                ExtractionStatus = extractionStatus,
                ExpectedExtractedTextLength = extractedTextLength,
                ExpectedExtractedTextSha256 = extractedTextSha256,
                FactEvidence = facts,
                HallucinationTraps = traps,
                AttachmentAbsolutePath = absolute,
                Source = "csv",
            });
        }

        return cases;
    }

    public static bool IsFixturePathSafe(string fixturePath)
    {
        // No absolute paths, no traversal.
        if (Path.IsPathRooted(fixturePath))
        {
            return false;
        }
        var normalized = fixturePath.Replace('\\', '/');
        return !normalized.Contains("../", StringComparison.Ordinal)
            && !normalized.StartsWith("./..", StringComparison.Ordinal);
    }

    private static string? FindAttachmentsRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName,
                "modules", "Unity.AI", "evals", "dataset", "attachments");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private static string? FindAncestorContaining(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, fileName)))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private static Dictionary<string, string> IndexAttachments(string attachmentsRoot)
    {
        var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(attachmentsRoot) || !Directory.Exists(attachmentsRoot))
        {
            return index;
        }

        foreach (var path in Directory.EnumerateFiles(attachmentsRoot))
        {
            var fileName = Path.GetFileName(path);
            var separator = fileName.IndexOf('_');
            if (separator <= 0)
            {
                continue;
            }

            var attachmentId = fileName[..separator];
            if (!Guid.TryParse(attachmentId, out _))
            {
                continue;
            }

            if (!index.TryAdd(attachmentId, path))
            {
                throw new InvalidDataException(
                    $"More than one downloaded file starts with attachment ID '{attachmentId}'.");
            }
        }

        return index;
    }

    private static List<List<string>> ReadCsv(string path)
    {
        var text = File.ReadAllText(path);
        var rows = new List<List<string>>();
        var field = new StringBuilder();
        var row = new List<string>();
        var inQuotes = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if (c == '\r')
                {
                    // swallow, handled by \n
                }
                else if (c == '\n')
                {
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = new List<string>();
                }
                else
                {
                    field.Append(c);
                }
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }

        if (inQuotes)
        {
            throw new InvalidDataException($"CSV '{path}' ends inside a quoted field.");
        }

        return rows;
    }

    private static Dictionary<string, int> BuildHeaderIndex(List<string> header)
    {
        var idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < header.Count; i++)
        {
            idx[header[i].Trim()] = i;
        }
        return idx;
    }

    private static void ValidateRequiredHeaders(Dictionary<string, int> headerIndex)
    {
        var missing = RequiredCsvHeaders
            .Where(header => !headerIndex.ContainsKey(header))
            .ToList();
        if (missing.Count > 0)
        {
            throw new InvalidDataException(
                $"CSV is missing required header(s): {string.Join(", ", missing)}.");
        }
    }

    private static string Field(List<string> row, Dictionary<string, int> idx, string name)
    {
        return idx.TryGetValue(name, out var i) && i < row.Count ? row[i].Trim() : "";
    }

    private static List<string> SplitList(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }
        return value.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
    }

    private static int ParseNonNegativeInt(string value, int rowNumber, string columnName)
    {
        if (int.TryParse(value, out var parsed) && parsed >= 0)
        {
            return parsed;
        }

        throw new InvalidDataException(
            $"CSV row {rowNumber} column '{columnName}' must be a non-negative integer.");
    }

    private static List<T> ParseJsonArray<T>(string value, int rowNumber, string columnName)
    {
        try
        {
            return JsonSerializer.Deserialize<List<T>>(value, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }) ?? new List<T>();
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"CSV row {rowNumber} column '{columnName}' must contain a valid JSON array.",
                exception);
        }
    }

    private static string? TagValue(IEnumerable<string> tags, string prefix)
    {
        var tag = tags.FirstOrDefault(
            candidate => candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        return tag is null ? null : tag[prefix.Length..];
    }

    private static void AddIfNonEmpty(List<string> list, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            list.Add(value.Trim());
        }
    }

    private static string ContentTypeForExtension(string extension, string fileName)
    {
        var ext = extension;
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = Path.GetExtension(fileName)?.TrimStart('.') ?? "";
        }
        return ext.ToLowerInvariant() switch
        {
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "xls" => "application/vnd.ms-excel",
            "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "ppt" => "application/vnd.ms-powerpoint",
            "pdf" => "application/pdf",
            "txt" => "text/plain",
            "csv" => "text/csv",
            _ => "application/octet-stream",
        };
    }
}
