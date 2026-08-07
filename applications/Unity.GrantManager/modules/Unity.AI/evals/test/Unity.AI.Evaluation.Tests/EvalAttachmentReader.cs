using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Extraction;
using Unity.AI.Operations;

namespace Unity.AI.Evaluation;

internal static class EvalAttachmentReader
{
    public static async Task<AttachmentExtraction> ExtractAsync(
        EvalCase evalCase,
        ITextExtractionService textExtractionService,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(evalCase.ExtractedText))
        {
            return new AttachmentExtraction(evalCase.ExtractedText, StoppedOnEmpty: false);
        }

        if (!string.IsNullOrWhiteSpace(evalCase.ExtractedTextPath))
        {
            EnsureSafeRelativePath(evalCase, evalCase.ExtractedTextPath);
            var extractedTextPath = Path.Combine(DatasetLoader.DatasetRoot, evalCase.ExtractedTextPath);
            if (!File.Exists(extractedTextPath))
            {
                throw new FileNotFoundException(
                    $"Extracted-text fixture for case '{evalCase.Id}' was not found.",
                    extractedTextPath);
            }

            var text = await File.ReadAllTextAsync(extractedTextPath, cancellationToken);
            return new AttachmentExtraction(text, StoppedOnEmpty: false);
        }

        string? absolutePath;
        if (!string.IsNullOrWhiteSpace(evalCase.FixturePath))
        {
            EnsureSafeRelativePath(evalCase, evalCase.FixturePath);
            absolutePath = Path.Combine(DatasetLoader.DatasetRoot, evalCase.FixturePath);
        }
        else
        {
            absolutePath = evalCase.AttachmentAbsolutePath;
        }

        if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
        {
            throw new FileNotFoundException(
                $"Attachment fixture for case '{evalCase.Id}' was not found.",
                absolutePath);
        }

        var contentType = string.IsNullOrWhiteSpace(evalCase.ContentType)
            ? "application/octet-stream"
            : evalCase.ContentType;

        await using var stream = File.OpenRead(absolutePath);
        return await AttachmentSummaryExtractor.ExtractAsync(
            evalCase.FileName,
            stream,
            contentType,
            textExtractionService,
            cancellationToken);
    }

    private static void EnsureSafeRelativePath(EvalCase evalCase, string path)
    {
        if (!DatasetLoader.IsFixturePathSafe(path))
        {
            throw new InvalidDataException(
                $"Case '{evalCase.Id}' contains unsafe fixture path '{path}'.");
        }
    }
}
