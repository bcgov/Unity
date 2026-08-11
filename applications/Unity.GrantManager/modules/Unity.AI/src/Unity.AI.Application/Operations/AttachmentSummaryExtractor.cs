using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Extraction;

namespace Unity.AI.Operations;

// Note: shared between production AttachmentSummaryService and evaluation harness.
// If a third caller shows up with a different empty-extraction policy, split the policy from the extract call.
public static class AttachmentSummaryExtractor
{
    public const string TextExtractionFailedSummary =
        "Attachment text could not be extracted for AI summary generation.";

    public static async Task<AttachmentExtraction> ExtractAsync(
        string fileName,
        Stream content,
        string contentType,
        ITextExtractionService textExtractionService,
        CancellationToken cancellationToken = default)
    {
        var extractedText = await textExtractionService.ExtractTextAsync(fileName, content, contentType, cancellationToken);
        var stoppedOnEmpty = string.IsNullOrWhiteSpace(extractedText) && IsSupportedOfficeOrPdf(fileName);
        return new AttachmentExtraction(extractedText, stoppedOnEmpty);
    }

    private static bool IsSupportedOfficeOrPdf(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return extension is ".pdf" or ".docx" or ".xlsx" or ".xls" or ".pptx";
    }
}

public sealed record AttachmentExtraction(string ExtractedText, bool StoppedOnEmpty);
