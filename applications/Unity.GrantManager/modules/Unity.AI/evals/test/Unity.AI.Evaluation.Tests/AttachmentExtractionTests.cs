using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Extraction;
using Xunit;

namespace Unity.AI.Evaluation;

[Trait("Category", "AIEvalOffline")]
public class AttachmentExtractionTests
{
    [Fact]
    public void Should_Match_All_Csv_Cases_When_Attachment_Dataset_Is_Present()
    {
        if (!Directory.Exists(DatasetLoader.AttachmentsRoot))
        {
            return;
        }

        var cases = DatasetLoader.LoadCsvCases(skipMissingAttachments: false);
        var missing = cases
            .Where(c => string.IsNullOrWhiteSpace(c.AttachmentAbsolutePath) ||
                        !File.Exists(c.AttachmentAbsolutePath))
            .Select(c => c.Id)
            .ToList();
        missing.ShouldBeEmpty();

        var caseIds = cases.Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var orphanIds = Directory.EnumerateFiles(DatasetLoader.AttachmentsRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name) && name!.Length > 36)
            .Select(name => name![..36])
            .Where(id => Guid.TryParse(id, out _) && !caseIds.Contains(id))
            .ToList();
        orphanIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Extract_Downloaded_Attachments_Using_Production_Policy()
    {
        if (!Directory.Exists(DatasetLoader.AttachmentsRoot))
        {
            return;
        }

        var extractor = new TextExtractionService(NullLogger<TextExtractionService>.Instance);
        var cases = DatasetLoader.LoadCsvCases(skipMissingAttachments: false);
        var failures = new System.Collections.Generic.List<string>();

        foreach (var evalCase in cases)
        {
            var extraction = await EvalAttachmentReader.ExtractAsync(
                evalCase,
                extractor,
                CancellationToken.None);
            var expectsNoText = evalCase.ExtractionStatus == "no_text_verified";

            if (expectsNoText)
            {
                if (!extraction.StoppedOnEmpty || !string.IsNullOrEmpty(extraction.ExtractedText))
                {
                    failures.Add(
                        $"{evalCase.Id}: expected the production empty-extraction short circuit.");
                }
            }
            else if (extraction.StoppedOnEmpty || string.IsNullOrWhiteSpace(extraction.ExtractedText))
            {
                failures.Add($"{evalCase.Id}: extracted no text.");
            }

            if (extraction.ExtractedText.Length > 50_000)
            {
                failures.Add(
                    $"{evalCase.Id}: extracted {extraction.ExtractedText.Length} characters; maximum is 50000.");
            }

            if (extraction.ExtractedText.Length != evalCase.ExpectedExtractedTextLength)
            {
                failures.Add(
                    $"{evalCase.Id}: extracted length changed from verified " +
                    $"{evalCase.ExpectedExtractedTextLength} to {extraction.ExtractedText.Length}.");
            }

            var actualHash = "sha256:" + Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(extraction.ExtractedText)))
                .ToLowerInvariant();
            if (!string.Equals(
                    actualHash,
                    evalCase.ExpectedExtractedTextSha256,
                    StringComparison.Ordinal))
            {
                failures.Add($"{evalCase.Id}: extracted text fingerprint changed.");
            }
        }

        failures.ShouldBeEmpty();
    }
}
