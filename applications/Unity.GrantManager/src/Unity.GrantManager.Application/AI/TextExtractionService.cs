using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XWPF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.AI
{
    public class TextExtractionService : ITextExtractionService, ITransientDependency
    {
        private const int MaxExtractedTextLength = 50000;
        private const int MaxExcelSheets = 10;
        private const int MaxExcelRowsPerSheet = 2000;
        private const int MaxExcelCellsPerRow = 50;
        private const int MaxDocxParagraphs = 2000;
        private const int MaxDocxTableRows = 2000;
        private const int MaxDocxTableCellsPerRow = 50;
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
                var normalizedContentType = contentType?.ToLowerInvariant() ?? string.Empty;
                var extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;

                string rawText;

                if (normalizedContentType.Contains("text/") ||
                    extension == ".txt" ||
                    extension == ".csv" ||
                    extension == ".json" ||
                    extension == ".xml")
                {
                    rawText = await ExtractTextFromTextFileAsync(fileContent);
                    return NormalizeAndLimitText(rawText, fileName);
                }

                if (normalizedContentType.Contains("pdf") || extension == ".pdf")
                {
                    rawText = ExtractTextFromPdfFile(fileName, fileContent);
                    return NormalizeAndLimitText(rawText, fileName);
                }

                if (normalizedContentType.Contains("word") ||
                    normalizedContentType.Contains("msword") ||
                    normalizedContentType.Contains("officedocument.wordprocessingml") ||
                    extension == ".doc" ||
                    extension == ".docx")
                {
                    if (extension == ".docx" || normalizedContentType.Contains("officedocument.wordprocessingml"))
                    {
                        rawText = ExtractTextFromWordDocx(fileContent);
                        return NormalizeAndLimitText(rawText, fileName);
                    }

                    _logger.LogDebug("Legacy .doc extraction is not supported for {FileName}", fileName);
                    return string.Empty;
                }

                if (normalizedContentType.Contains("excel") ||
                    normalizedContentType.Contains("spreadsheet") ||
                    extension == ".xls" ||
                    extension == ".xlsx")
                {
                    rawText = ExtractTextFromExcelFile(fileName, fileContent);
                    return NormalizeAndLimitText(rawText, fileName);
                }

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
                var text = Encoding.UTF8.GetString(fileContent);

                if (text.Contains('\uFFFD'))
                {
                    text = Encoding.ASCII.GetString(fileContent);
                }

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

                foreach (var page in document.GetPages())
                {
                    if (builder.Length >= MaxExtractedTextLength)
                    {
                        break;
                    }

                    if (!string.IsNullOrWhiteSpace(page.Text))
                    {
                        builder.AppendLine(page.Text);
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

        private string ExtractTextFromWordDocx(byte[] fileContent)
        {
            try
            {
                using var stream = new MemoryStream(fileContent, writable: false);
                using var document = new XWPFDocument(stream);
                var parts = new List<string>();

                foreach (var paragraphText in document.Paragraphs.Take(MaxDocxParagraphs).Select(paragraph => paragraph.ParagraphText))
                {
                    if (!string.IsNullOrWhiteSpace(paragraphText))
                    {
                        parts.Add(paragraphText);
                    }
                }

                foreach (var table in document.Tables)
                {
                    foreach (var row in table.Rows.Take(MaxDocxTableRows))
                    {
                        foreach (var cell in row.GetTableCells().Take(MaxDocxTableCellsPerRow))
                        {
                            var text = cell.GetText();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                parts.Add(text);
                            }
                        }
                    }
                }

                var combined = string.Join(Environment.NewLine, parts);
                if (combined.Length > MaxExtractedTextLength)
                {
                    combined = combined.Substring(0, MaxExtractedTextLength);
                }

                return combined;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Word (.docx) text extraction failed");
                return string.Empty;
            }
        }

        private string ExtractTextFromExcelFile(string fileName, byte[] fileContent)
        {
            try
            {
                using var stream = new MemoryStream(fileContent, writable: false);
                using var workbook = WorkbookFactory.Create(stream);
                var rows = new List<string>();
                var totalLength = 0;
                var sheetCount = Math.Min(workbook.NumberOfSheets, MaxExcelSheets);

                for (var sheetIndex = 0; sheetIndex < sheetCount; sheetIndex++)
                {
                    var sheet = workbook.GetSheetAt(sheetIndex);
                    if (sheet == null)
                    {
                        continue;
                    }

                    var processedRows = 0;
                    foreach (IRow row in sheet)
                    {
                        if (processedRows >= MaxExcelRowsPerSheet || totalLength >= MaxExtractedTextLength)
                        {
                            break;
                        }

                        var cellTexts = row.Cells
                            .Take(MaxExcelCellsPerRow)
                            .Select(GetCellText)
                            .Where(value => !string.IsNullOrWhiteSpace(value))
                            .ToList();

                        processedRows++;

                        if (cellTexts.Count == 0)
                        {
                            continue;
                        }

                        var rowText = string.Join(" | ", cellTexts);
                        rows.Add(rowText);
                        totalLength += rowText.Length;
                    }

                    if (totalLength >= MaxExtractedTextLength)
                    {
                        break;
                    }
                }

                var combined = string.Join(Environment.NewLine, rows);
                if (combined.Length > MaxExtractedTextLength)
                {
                    combined = combined.Substring(0, MaxExtractedTextLength);
                }

                return combined;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Excel text extraction failed for {FileName}", fileName);
                return string.Empty;
            }
        }

        private static string GetCellText(NPOI.SS.UserModel.ICell cell)
        {
            if (cell == null)
            {
                return string.Empty;
            }

            return (cell.CellType switch
            {
                CellType.String => cell.StringCellValue ?? string.Empty,
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                    ? cell.DateCellValue.ToString()
                    : cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue ? "true" : "false",
                CellType.Formula => cell.ToString(),
                CellType.Blank => string.Empty,
                _ => cell.ToString() ?? string.Empty
            }) ?? string.Empty;
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

            normalized = Regex.Replace(normalized, @"(?<=[a-z])(?=[A-Z])", " ");
            normalized = Regex.Replace(normalized, @"(?<=[\.\,\:\;\)])(?=[A-Za-z0-9])", " ");
            normalized = Regex.Replace(normalized, @":-", ": - ");
            normalized = Regex.Replace(normalized, @"(?<=\S)- (?=[A-Za-z])", " - ");
            normalized = Regex.Replace(
                normalized,
                @"(?<=[a-z])(?=(project|funding|budget|community|summary|notes|details|planning|outcomes|background|services)\b)",
                " ",
                RegexOptions.IgnoreCase);
            normalized = Regex.Replace(normalized, @"[ \t]+", " ");
            normalized = Regex.Replace(normalized, @"\n\s*", "\n");
            normalized = Regex.Replace(normalized, @"\n{2,}", "\n");

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
    }
}
