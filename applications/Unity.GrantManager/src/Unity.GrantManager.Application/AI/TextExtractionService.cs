using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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

                // Handle text-based files
                if (normalizedContentType.Contains("text/") ||
                    extension == ".txt" ||
                    extension == ".csv" ||
                    extension == ".json" ||
                    extension == ".xml")
                {
                    return await ExtractTextFromTextFileAsync(fileContent);
                }

                // Handle PDF files
                if (normalizedContentType.Contains("pdf") || extension == ".pdf")
                {
                    // For now, return empty string - can be enhanced with PDF parsing library
                    _logger.LogDebug("PDF text extraction not yet implemented for {FileName}", fileName);
                    return string.Empty;
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

                // Limit the extracted text to a reasonable size (e.g., first 50,000 characters)
                const int maxLength = 50000;
                if (text.Length > maxLength)
                {
                    text = text.Substring(0, maxLength);
                    _logger.LogDebug("Truncated text content to {MaxLength} characters", maxLength);
                }

                return await Task.FromResult(text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding text file");
                return string.Empty;
            }
        }
    }
}
