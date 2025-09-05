using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.AI
{
    public class TextExtractionService : ITextExtractionService, ITransientDependency
    {
        private readonly ILogger<TextExtractionService> _logger;

        public TextExtractionService(ILogger<TextExtractionService> logger)
        {
            _logger = logger;
        }

        public bool IsExtractionSupported(string fileName, string contentType)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var type = contentType.ToLowerInvariant();

            return extension switch
            {
                ".pdf" => true,
                ".docx" => true,
                ".doc" => false, // Legacy .doc format not supported (requires different library)
                ".txt" => true,
                ".rtf" => false, // Could add RTF support later
                _ => type switch
                {
                    "application/pdf" => true,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => true,
                    "text/plain" => true,
                    _ => false
                }
            };
        }

        public async Task<string?> ExtractTextAsync(string fileName, byte[] fileContent, string contentType)
        {
            if (fileContent == null || fileContent.Length == 0)
            {
                return null;
            }

            try
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                
                return extension switch
                {
                    ".pdf" => await ExtractFromPdfAsync(fileContent),
                    ".docx" => await ExtractFromDocxAsync(fileContent),
                    ".txt" => await ExtractFromTextAsync(fileContent),
                    _ => contentType.ToLowerInvariant() switch
                    {
                        "application/pdf" => await ExtractFromPdfAsync(fileContent),
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => await ExtractFromDocxAsync(fileContent),
                        "text/plain" => await ExtractFromTextAsync(fileContent),
                        _ => null
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting text from file {FileName}", fileName);
                return null;
            }
        }

        private async Task<string?> ExtractFromPdfAsync(byte[] fileContent)
        {
            try
            {
                using var stream = new MemoryStream(fileContent);
                using var document = PdfDocument.Open(stream);
                
                var textBuilder = new StringBuilder();
                
                foreach (var page in document.GetPages())
                {
                    var pageText = page.Text;
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        textBuilder.AppendLine(pageText);
                    }
                }

                var extractedText = textBuilder.ToString().Trim();
                
                // Limit text length to prevent token overuse
                if (extractedText.Length > 10000)
                {
                    extractedText = extractedText.Substring(0, 10000) + "... [truncated]";
                }

                return string.IsNullOrWhiteSpace(extractedText) ? null : extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting text from PDF");
                return null;
            }
        }

        private async Task<string?> ExtractFromDocxAsync(byte[] fileContent)
        {
            try
            {
                using var stream = new MemoryStream(fileContent);
                using var document = WordprocessingDocument.Open(stream, false);
                
                var body = document.MainDocumentPart?.Document?.Body;
                if (body == null)
                {
                    return null;
                }

                var textBuilder = new StringBuilder();
                
                // Extract text from paragraphs
                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    var paragraphText = paragraph.InnerText;
                    if (!string.IsNullOrWhiteSpace(paragraphText))
                    {
                        textBuilder.AppendLine(paragraphText);
                    }
                }

                // Extract text from tables
                foreach (var table in body.Elements<Table>())
                {
                    foreach (var row in table.Elements<TableRow>())
                    {
                        var rowTexts = row.Elements<TableCell>().Select(cell => cell.InnerText).Where(text => !string.IsNullOrWhiteSpace(text));
                        if (rowTexts.Any())
                        {
                            textBuilder.AppendLine(string.Join(" | ", rowTexts));
                        }
                    }
                }

                var extractedText = textBuilder.ToString().Trim();
                
                // Limit text length to prevent token overuse
                if (extractedText.Length > 10000)
                {
                    extractedText = extractedText.Substring(0, 10000) + "... [truncated]";
                }

                return string.IsNullOrWhiteSpace(extractedText) ? null : extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting text from DOCX");
                return null;
            }
        }

        private async Task<string?> ExtractFromTextAsync(byte[] fileContent)
        {
            try
            {
                var text = Encoding.UTF8.GetString(fileContent).Trim();
                
                // Limit text length to prevent token overuse
                if (text.Length > 10000)
                {
                    text = text.Substring(0, 10000) + "... [truncated]";
                }

                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting text from plain text file");
                return null;
            }
        }
    }
}