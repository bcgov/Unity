using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.AI
{
    public partial class TextExtractionService : ITextExtractionService, ITransientDependency
    {
        private const int MaxExtractedTextLength = 50000;
        private readonly ILogger<TextExtractionService> _logger;

        public TextExtractionService(ILogger<TextExtractionService> logger)
        {
            _logger = logger;
        }

        public async Task<string> ExtractTextAsync(string fileName, byte[] fileContent, string contentType)
        {
            if (fileContent == null || fileContent.Length == 0)
            {
                _logger.LogDebug("File content is empty for {FileName}", fileName);
                return string.Empty;
            }

            try
            {
                // Normalize content type
                var normalizedContentType = contentType?.ToLowerInvariant() ?? string.Empty;
                var extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;

                string rawText;

                // Handle text-based files
                if (normalizedContentType.Contains("text/") ||
                    extension == ".txt" ||
                    extension == ".csv" ||
                    extension == ".json" ||
                    extension == ".xml")
                {
                    rawText = await ExtractTextFromTextFileAsync(fileContent);
                    return NormalizeAndLimitText(rawText, fileName);
                }

                // Handle PDF files
                if (normalizedContentType.Contains("pdf") || extension == ".pdf")
                {
                    rawText = await Task.FromResult(ExtractTextFromPdfFile(fileName, fileContent));
                    return NormalizeAndLimitText(rawText, fileName);
                }

                // Handle Word documents
                if (normalizedContentType.Contains("word") ||
                    normalizedContentType.Contains("msword") ||
                    normalizedContentType.Contains("officedocument.wordprocessingml") ||
                    extension == ".doc" ||
                    extension == ".docx")
                {
                    // For now, return empty string - can be enhanced with Word parsing library
                    _logger.LogDebug("Word document text extraction not yet implemented for {FileName}", fileName);
                    return string.Empty;
                }

                // Handle Excel files
                if (normalizedContentType.Contains("excel") ||
                    normalizedContentType.Contains("spreadsheet") ||
                    extension == ".xls" ||
                    extension == ".xlsx")
                {
                    // For now, return empty string - can be enhanced with Excel parsing library
                    _logger.LogDebug("Excel text extraction not yet implemented for {FileName}", fileName);
                    return string.Empty;
                }

                // For other file types, return empty string
                _logger.LogDebug("No text extraction available for content type {ContentType} with extension {Extension}",
                    contentType, extension);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from {FileName}", fileName);
                return string.Empty;
            }
        }

        private async Task<string> ExtractTextFromTextFileAsync(byte[] fileContent)
        {
            try
            {
                // Try UTF-8 first
                var text = Encoding.UTF8.GetString(fileContent);

                // Check if the decoded text contains replacement characters (indicates encoding issue)
                if (text.Contains('\uFFFD'))
                {
                    // Try other encodings
                    text = Encoding.ASCII.GetString(fileContent);
                }

                // Limit the extracted text to a reasonable size.
                if (text.Length > MaxExtractedTextLength)
                {
                    text = text.Substring(0, MaxExtractedTextLength);
                    _logger.LogDebug("Truncated text content to {MaxLength} characters", MaxExtractedTextLength);
                }

                return await Task.FromResult(text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding text file");
                return string.Empty;
            }
        }

        private string ExtractTextFromPdfFile(string fileName, byte[] fileContent)
        {
            try
            {
                using var stream = new MemoryStream(fileContent, writable: false);
                using var document = PdfDocument.Open(stream);
                var builder = new StringBuilder();

                foreach (var pageText in document.GetPages().Select(page => page.Text))
                {
                    if (builder.Length >= MaxExtractedTextLength)
                    {
                        break;
                    }

                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        builder.AppendLine(pageText);
                    }
                }

                var text = builder.ToString();
                if (text.Length > MaxExtractedTextLength)
                {
                    text = text.Substring(0, MaxExtractedTextLength);
                }

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PDF text extraction failed for {FileName}", fileName);
                return string.Empty;
            }
        }

        private string NormalizeAndLimitText(string text, string fileName)
        {
            var normalized = NormalizeExtractedText(text);
            normalized = RemoveLeadingFileNameArtifact(normalized, fileName);

            if (normalized.Length > MaxExtractedTextLength)
            {
                normalized = normalized.Substring(0, MaxExtractedTextLength);
                _logger.LogDebug("Truncated extracted content to {MaxLength} characters", MaxExtractedTextLength);
            }

            return normalized;
        }

        private static string NormalizeExtractedText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalized = text
                .Replace('\0', ' ')
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');

            normalized = LowerToUpperWordBoundaryRegex().Replace(normalized, " ");
            normalized = PunctuationToWordBoundaryRegex().Replace(normalized, " ");
            normalized = ColonDashSpacingRegex().Replace(normalized, ": - ");
            normalized = HyphenSpacingRegex().Replace(normalized, " - ");
            normalized = KeywordBoundaryRegex().Replace(normalized, " ");
            normalized = MultipleSpacesRegex().Replace(normalized, " ");
            normalized = NewlineWhitespaceRegex().Replace(normalized, "\n");
            normalized = MultipleNewlinesRegex().Replace(normalized, "\n");

            return normalized.Trim();
        }

        private static string RemoveLeadingFileNameArtifact(string text, string fileName)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(fileName))
            {
                return text;
            }

            var rawStem = Path.GetFileNameWithoutExtension(fileName)?.Trim();
            if (string.IsNullOrWhiteSpace(rawStem))
            {
                return text;
            }

            var decodedStem = Uri.UnescapeDataString(rawStem);
            foreach (var candidate in new[] { rawStem, decodedStem })
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                if (text.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    var stripped = text.Substring(candidate.Length).TrimStart(' ', '-', ':', '.', '\t');
                    if (!string.IsNullOrWhiteSpace(stripped))
                    {
                        return stripped;
                    }
                }
            }

            return text;
        }

        [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])")]
        private static partial Regex LowerToUpperWordBoundaryRegex();

        [GeneratedRegex(@"(?<=[\.\,\:\;\)])(?=[A-Za-z0-9])")]
        private static partial Regex PunctuationToWordBoundaryRegex();

        [GeneratedRegex(@":-")]
        private static partial Regex ColonDashSpacingRegex();

        [GeneratedRegex(@"(?<=\S)- (?=[A-Za-z])")]
        private static partial Regex HyphenSpacingRegex();

        [GeneratedRegex(@"(?<=[a-z])(?=(project|funding|budget|community|summary|notes|details|planning|outcomes|background|services)\b)", RegexOptions.IgnoreCase)]
        private static partial Regex KeywordBoundaryRegex();

        [GeneratedRegex(@"[ \t]+")]
        private static partial Regex MultipleSpacesRegex();

        [GeneratedRegex(@"\n\s*")]
        private static partial Regex NewlineWhitespaceRegex();

        [GeneratedRegex(@"\n{2,}")]
        private static partial Regex MultipleNewlinesRegex();
    }
}
