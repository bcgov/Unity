using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XWPF.UserModel;
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

        public Task<string> ExtractTextAsync(string fileName, byte[] fileContent, string contentType)
        {
            if (fileContent == null || fileContent.Length == 0)
            {
                _logger.LogDebug("File content is empty for {FileName}", fileName);
                return Task.FromResult(string.Empty);
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
                    rawText = ExtractTextFromTextFile(fileContent);
                    return Task.FromResult(NormalizeAndLimitText(rawText, fileName));
                }

                if (normalizedContentType.Contains("pdf") || extension == ".pdf")
                {
                    rawText = ExtractTextFromPdfFile(fileName, fileContent);
                    return Task.FromResult(NormalizeAndLimitText(rawText, fileName));
                }

                if (normalizedContentType.Contains("word") ||
                    normalizedContentType.Contains("msword") ||
                    normalizedContentType.Contains("officedocument.wordprocessingml") ||
                    extension == ".doc" ||
                    extension == ".docx")
                {
                    if (extension == ".docx" || normalizedContentType.Contains("officedocument.wordprocessingml"))
                    {
                        rawText = ExtractTextFromWordDocx(fileName, fileContent);
                        return Task.FromResult(NormalizeAndLimitText(rawText, fileName));
                    }

                    _logger.LogDebug("Legacy .doc extraction is not supported for {FileName}", fileName);
                    return Task.FromResult(string.Empty);
                }

                if (normalizedContentType.Contains("excel") ||
                    normalizedContentType.Contains("spreadsheet") ||
                    extension == ".xls" ||
                    extension == ".xlsx")
                {
                    rawText = ExtractTextFromExcelFile(fileName, fileContent);
                    return Task.FromResult(NormalizeAndLimitText(rawText, fileName));
                }

                _logger.LogDebug("No text extraction available for content type {ContentType} with extension {Extension}",
                    contentType, extension);
                return Task.FromResult(string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from {FileName}", fileName);
                return Task.FromResult(string.Empty);
            }
        }

        private string ExtractTextFromTextFile(byte[] fileContent)
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

                return text;
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
                var pageTexts = document.GetPages()
                    .Where(page => !string.IsNullOrWhiteSpace(page.Text))
                    .Select(page => page.Text);

                foreach (var pageText in pageTexts)
                {
                    if (TryAppendWithTrailingNewline(builder, pageText))
                    {
                        break;
                    }
                }

                return builder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PDF text extraction failed for {FileName}", fileName);
                return string.Empty;
            }
        }

        private string ExtractTextFromWordDocx(string fileName, byte[] fileContent)
        {
            try
            {
                using var stream = new MemoryStream(fileContent, writable: false);
                using var document = new XWPFDocument(stream);
                var builder = new StringBuilder();
                var paragraphTexts = document.Paragraphs
                    .Take(MaxDocxParagraphs)
                    .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph.ParagraphText))
                    .Select(paragraph => paragraph.ParagraphText);

                foreach (var paragraphText in paragraphTexts)
                {
                    if (TryAppendWithTrailingNewline(builder, paragraphText))
                    {
                        break;
                    }
                }

                TryAppendDocxTableText(document, builder);

                return builder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Word (.docx) text extraction failed for {FileName}", fileName);
                return string.Empty;
            }
        }

        private static void TryAppendDocxTableText(XWPFDocument document, StringBuilder builder)
        {
            if (builder.Length >= MaxExtractedTextLength)
            {
                return;
            }

            foreach (var table in document.Tables)
            {
                foreach (var row in table.Rows.Take(MaxDocxTableRows))
                {
                    var cellTexts = row.GetTableCells()
                        .Take(MaxDocxTableCellsPerRow)
                        .Where(cell => !string.IsNullOrWhiteSpace(cell.GetText()))
                        .Select(cell => cell.GetText());

                    foreach (var cellText in cellTexts)
                    {
                        if (TryAppendWithTrailingNewline(builder, cellText))
                        {
                            return;
                        }
                    }
                }
            }
        }

        private string ExtractTextFromExcelFile(string fileName, byte[] fileContent)
        {
            try
            {
                using var stream = new MemoryStream(fileContent, writable: false);
                using var workbook = WorkbookFactory.Create(stream);
                var builder = new StringBuilder();
                var sheetCount = Math.Min(workbook.NumberOfSheets, MaxExcelSheets);

                for (var sheetIndex = 0; sheetIndex < sheetCount; sheetIndex++)
                {
                    if (builder.Length >= MaxExtractedTextLength)
                    {
                        break;
                    }

                    var sheet = workbook.GetSheetAt(sheetIndex);
                    var limitReached = TryAppendExcelSheet(sheet, builder);
                    if (limitReached)
                    {
                        break;
                    }
                }

                return builder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Excel text extraction failed for {FileName}", fileName);
                return string.Empty;
            }
        }

        private static bool TryAppendExcelSheet(ISheet? sheet, StringBuilder builder)
        {
            if (sheet == null)
            {
                return false;
            }

            var processedRows = 0;
            foreach (IRow row in sheet)
            {
                if (processedRows >= MaxExcelRowsPerSheet || builder.Length >= MaxExtractedTextLength)
                {
                    break;
                }

                var limitReached = TryAppendExcelRow(row, builder);
                processedRows++;
                if (limitReached)
                {
                    return true;
                }
            }

            return builder.Length >= MaxExtractedTextLength;
        }

        private static bool TryAppendExcelRow(IRow row, StringBuilder builder)
        {
            var rowHasValue = false;
            foreach (var cell in row.Cells.Take(MaxExcelCellsPerRow))
            {
                var value = GetCellText(cell);
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                string? separator = null;
                if (rowHasValue)
                {
                    separator = " | ";
                }

                var limitReached = AppendWithLimit(builder, value, MaxExtractedTextLength, separator);
                rowHasValue = true;
                if (limitReached)
                {
                    return true;
                }
            }

            if (rowHasValue &&
                builder.Length + Environment.NewLine.Length <= MaxExtractedTextLength)
            {
                builder.Append(Environment.NewLine);
            }

            return builder.Length >= MaxExtractedTextLength;
        }

        private static bool TryAppendWithTrailingNewline(StringBuilder builder, string? value)
        {
            var limitReached = AppendWithLimit(builder, value, MaxExtractedTextLength);
            if (limitReached)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                AppendTrailingNewlineIfRoom(builder);
            }

            return builder.Length >= MaxExtractedTextLength;
        }

        private static void AppendTrailingNewlineIfRoom(StringBuilder builder)
        {
            if (builder.Length > 0 &&
                builder.Length + Environment.NewLine.Length <= MaxExtractedTextLength)
            {
                builder.Append(Environment.NewLine);
            }
        }

        private static bool AppendWithLimit(StringBuilder builder, string? value, int maxLength, string? separator = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return builder.Length >= maxLength;
            }

            if (builder.Length >= maxLength)
            {
                return true;
            }

            var remaining = maxLength - builder.Length;
            if (remaining <= 0)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(separator) && builder.Length > 0)
            {
                if (separator.Length >= remaining)
                {
                    builder.Append(separator.AsSpan(0, remaining));
                    return true;
                }

                builder.Append(separator);
                remaining -= separator.Length;
            }

            if (value.Length >= remaining)
            {
                builder.Append(value.AsSpan(0, remaining));
                return true;
            }

            builder.Append(value);
            return false;
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
