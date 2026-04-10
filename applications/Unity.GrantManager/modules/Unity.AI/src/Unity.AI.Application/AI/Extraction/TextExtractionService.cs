using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XWPF.UserModel;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using UglyToad.PdfPig;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Extraction
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
        private const int MaxPowerPointSlides = 200;
        private readonly ILogger<TextExtractionService> _logger;
        private readonly Dictionary<string, Func<string, byte[], string>> _extractorsByExtension;

        public TextExtractionService(ILogger<TextExtractionService> logger)
        {
            _logger = logger;
            _extractorsByExtension = new Dictionary<string, Func<string, byte[], string>>(StringComparer.OrdinalIgnoreCase)
            {
                [".txt"] = (_, content) => ExtractTextFromTextFile(content),
                [".csv"] = (_, content) => ExtractTextFromTextFile(content),
                [".json"] = (_, content) => ExtractTextFromTextFile(content),
                [".xml"] = (_, content) => ExtractTextFromTextFile(content),
                [".pdf"] = ExtractTextFromPdfFile,
                [".docx"] = ExtractTextFromWordDocx,
                [".xls"] = ExtractTextFromExcelFile,
                [".xlsx"] = ExtractTextFromExcelFile,
                [".pptx"] = ExtractTextFromPowerPointFile
            };
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

                if (extension == ".doc")
                {
                    _logger.LogDebug("Legacy .doc extraction is not supported for {FileName}", fileName);
                    return Task.FromResult(string.Empty);
                }

                if (_extractorsByExtension.TryGetValue(extension, out var extractor))
                {
                    var rawText = extractor(fileName, fileContent);
                    return Task.FromResult(NormalizeAndLimitText(rawText, fileName));
                }

                if (normalizedContentType.Contains("text/"))
                {
                    var rawText = ExtractTextFromTextFile(fileContent);
                    return Task.FromResult(NormalizeAndLimitText(rawText, fileName));
                }

                if (normalizedContentType.Contains("pdf"))
                {
                    var rawText = ExtractTextFromPdfFile(fileName, fileContent);
                    return Task.FromResult(NormalizeAndLimitText(rawText, fileName));
                }

                if (normalizedContentType.Contains("word") ||
                    normalizedContentType.Contains("msword") ||
                    normalizedContentType.Contains("officedocument.wordprocessingml"))
                {
                    var rawText = ExtractTextFromWordDocx(fileName, fileContent);
                    return Task.FromResult(NormalizeAndLimitText(rawText, fileName));
                }

                if (normalizedContentType.Contains("excel") || normalizedContentType.Contains("spreadsheet"))
                {
                    var rawText = ExtractTextFromExcelFile(fileName, fileContent);
                    return Task.FromResult(NormalizeAndLimitText(rawText, fileName));
                }

                if (normalizedContentType.Contains("presentation") ||
                    normalizedContentType.Contains("powerpoint"))
                {
                    var rawText = ExtractTextFromPowerPointFile(fileName, fileContent);
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

                _logger.LogDebug("Extracted {CharacterCount} characters from text-based content.", text.Length);
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
                var processedPageCount = 0;
                var pageTexts = document.GetPages()
                    .Select(page => page.Text)
                    .Where(pageText => !string.IsNullOrWhiteSpace(pageText));

                foreach (var pageText in pageTexts)
                {
                    if (builder.Length >= MaxExtractedTextLength)
                    {
                        break;
                    }

                    processedPageCount++;
                    if (TryAppendWithTrailingNewline(builder, pageText))
                    {
                        break;
                    }
                }

                _logger.LogDebug("Extracted PDF text from {ProcessedPageCount} pages for {FileName}", processedPageCount, fileName);
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
                var processedParagraphCount = AppendDocxParagraphText(document, builder);
                var processedTableRowCount = AppendDocxTableText(document, builder);

                _logger.LogDebug(
                    "Extracted Word text from {ProcessedParagraphCount} paragraphs and {ProcessedTableRowCount} table rows for {FileName}",
                    processedParagraphCount,
                    processedTableRowCount,
                    fileName);
                return builder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Word (.docx) text extraction failed for {FileName}", fileName);
                return string.Empty;
            }
        }

        private static int AppendDocxParagraphText(XWPFDocument document, StringBuilder builder)
        {
            var processedParagraphCount = 0;
            var paragraphTexts = document.Paragraphs
                .Take(MaxDocxParagraphs)
                .Select(paragraph => paragraph.ParagraphText)
                .Where(paragraphText => !string.IsNullOrWhiteSpace(paragraphText));

            foreach (var paragraphText in paragraphTexts)
            {
                if (builder.Length >= MaxExtractedTextLength)
                {
                    break;
                }

                processedParagraphCount++;
                if (TryAppendWithTrailingNewline(builder, paragraphText))
                {
                    break;
                }
            }

            return processedParagraphCount;
        }

        private static int AppendDocxTableText(XWPFDocument document, StringBuilder builder)
        {
            if (builder.Length >= MaxExtractedTextLength)
            {
                return 0;
            }

            var processedTableRowCount = 0;
            foreach (var table in document.Tables)
            {
                foreach (var row in table.Rows.Take(MaxDocxTableRows))
                {
                    if (builder.Length >= MaxExtractedTextLength)
                    {
                        return processedTableRowCount;
                    }

                    var cellTexts = row.GetTableCells()
                        .Take(MaxDocxTableCellsPerRow)
                        .Select(cell => cell.GetText())
                        .Where(cellText => !string.IsNullOrWhiteSpace(cellText));

                    var rowHadValue = false;
                    foreach (var cellText in cellTexts)
                    {
                        rowHadValue = true;
                        if (TryAppendWithTrailingNewline(builder, cellText))
                        {
                            return processedTableRowCount + 1;
                        }
                    }

                    if (rowHadValue)
                    {
                        processedTableRowCount++;
                    }
                }
            }

            return processedTableRowCount;
        }

        private string ExtractTextFromExcelFile(string fileName, byte[] fileContent)
        {
            try
            {
                using var stream = new MemoryStream(fileContent, writable: false);
                using var workbook = WorkbookFactory.Create(stream);
                var builder = new StringBuilder();
                var sheetCount = Math.Min(workbook.NumberOfSheets, MaxExcelSheets);
                var processedSheetCount = 0;
                var processedRowCount = 0;

                for (var sheetIndex = 0; sheetIndex < sheetCount; sheetIndex++)
                {
                    if (builder.Length >= MaxExtractedTextLength)
                    {
                        break;
                    }

                    var sheet = workbook.GetSheetAt(sheetIndex);
                    var (rowsProcessed, limitReached) = TryAppendExcelSheet(sheet, builder);
                    if (rowsProcessed > 0)
                    {
                        processedSheetCount++;
                        processedRowCount += rowsProcessed;
                    }

                    if (limitReached)
                    {
                        break;
                    }
                }

                _logger.LogDebug(
                    "Extracted Excel text from {ProcessedSheetCount} sheets and {ProcessedRowCount} rows for {FileName}",
                    processedSheetCount,
                    processedRowCount,
                    fileName);
                return builder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Excel text extraction failed for {FileName}", fileName);
                return string.Empty;
            }
        }

        private string ExtractTextFromPowerPointFile(string fileName, byte[] fileContent)
        {
            try
            {
                using var stream = new MemoryStream(fileContent, writable: false);
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
                var builder = new StringBuilder();
                var slideEntries = GetOrderedPowerPointSlideEntries(archive)
                    .Take(MaxPowerPointSlides);
                var processedSlideCount = 0;

                foreach (var slideEntry in slideEntries)
                {
                    if (builder.Length >= MaxExtractedTextLength)
                    {
                        break;
                    }

                    using var slideStream = slideEntry.Open();
                    var slideText = ExtractPowerPointSlideText(slideStream);
                    if (string.IsNullOrWhiteSpace(slideText))
                    {
                        continue;
                    }

                    processedSlideCount++;
                    if (TryAppendWithTrailingNewline(builder, slideText))
                    {
                        break;
                    }
                }

                _logger.LogDebug("Extracted PowerPoint text from {ProcessedSlideCount} slides for {FileName}", processedSlideCount, fileName);
                return builder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PowerPoint (.pptx) text extraction failed for {FileName}", fileName);
                return string.Empty;
            }
        }

        private IEnumerable<ZipArchiveEntry> GetOrderedPowerPointSlideEntries(ZipArchive archive)
        {
            var slideEntriesByName = archive.Entries
                .Where(entry => entry.FullName.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase) &&
                                entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(entry => entry.FullName, StringComparer.OrdinalIgnoreCase);

            if (slideEntriesByName.Count == 0)
            {
                _logger.LogDebug("No slide entries found in PowerPoint archive.");
                return Enumerable.Empty<ZipArchiveEntry>();
            }

            var orderedSlideNames = TryGetPowerPointSlideOrder(archive);
            if (orderedSlideNames.Count == 0)
            {
                _logger.LogDebug("Using PowerPoint part-name order fallback for {SlideCount} slides.", slideEntriesByName.Count);
                return slideEntriesByName.Values
                    .OrderBy(entry => GetPowerPointSlideNumber(entry.FullName))
                    .ToList();
            }

            var orderedEntries = new List<ZipArchiveEntry>(slideEntriesByName.Count);
            foreach (var slideName in orderedSlideNames)
            {
                if (slideEntriesByName.TryGetValue(slideName, out var slideEntry))
                {
                    orderedEntries.Add(slideEntry);
                    slideEntriesByName.Remove(slideName);
                }
            }

            if (slideEntriesByName.Count > 0)
            {
                orderedEntries.AddRange(slideEntriesByName.Values.OrderBy(entry => GetPowerPointSlideNumber(entry.FullName)));
            }

            _logger.LogDebug("Resolved PowerPoint presentation order for {SlideCount} slides.", orderedEntries.Count);
            return orderedEntries;
        }

        private static (int RowsProcessed, bool LimitReached) TryAppendExcelSheet(ISheet? sheet, StringBuilder builder)
        {
            if (sheet == null)
            {
                return (0, false);
            }

            var processedRows = 0;
            foreach (IRow row in sheet)
            {
                if (processedRows >= MaxExcelRowsPerSheet || builder.Length >= MaxExtractedTextLength)
                {
                    break;
                }

                var (rowHadValue, limitReached) = TryAppendExcelRow(row, builder);
                if (rowHadValue)
                {
                    processedRows++;
                }

                if (limitReached)
                {
                    return (processedRows, true);
                }
            }

            return (processedRows, builder.Length >= MaxExtractedTextLength);
        }

        private static (bool RowHadValue, bool LimitReached) TryAppendExcelRow(IRow row, StringBuilder builder)
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
                    return (true, true);
                }
            }

            if (rowHasValue &&
                builder.Length + Environment.NewLine.Length <= MaxExtractedTextLength)
            {
                builder.Append(Environment.NewLine);
            }

            return (rowHasValue, builder.Length >= MaxExtractedTextLength);
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

        private static string ExtractPowerPointSlideText(Stream slideStream)
        {
            var document = XDocument.Load(slideStream);
            XNamespace drawingNamespace = "http://schemas.openxmlformats.org/drawingml/2006/main";
            var textRuns = document
                .Descendants(drawingNamespace + "t")
                .Select(node => node.Value?.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value));

            return string.Join(Environment.NewLine, textRuns);
        }

        private static int GetPowerPointSlideNumber(string entryName)
        {
            var fileName = Path.GetFileNameWithoutExtension(entryName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return int.MaxValue;
            }

            var slideNumberText = fileName.Substring("slide".Length);
            return int.TryParse(slideNumberText, out var slideNumber)
                ? slideNumber
                : int.MaxValue;
        }

        private List<string> TryGetPowerPointSlideOrder(ZipArchive archive)
        {
            try
            {
                var presentationEntry = archive.GetEntry("ppt/presentation.xml");
                var relationshipsEntry = archive.GetEntry("ppt/_rels/presentation.xml.rels");
                if (presentationEntry == null || relationshipsEntry == null)
                {
                    return new List<string>();
                }

                using var presentationStream = presentationEntry.Open();
                using var relationshipsStream = relationshipsEntry.Open();
                var presentationDocument = XDocument.Load(presentationStream);
                var relationshipsDocument = XDocument.Load(relationshipsStream);

                XNamespace presentationNamespace = "http://schemas.openxmlformats.org/presentationml/2006/main";
                XNamespace officeDocumentRelationshipsNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
                XNamespace packageRelationshipsNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";

                var slideTargetsByRelationshipId = (relationshipsDocument
                    .Root?
                    .Elements(packageRelationshipsNamespace + "Relationship")
                    .Where(element => string.Equals(
                        element.Attribute("Type")?.Value,
                        "http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide",
                        StringComparison.OrdinalIgnoreCase))
                    .Select(element => new
                    {
                        Id = element.Attribute("Id")?.Value,
                        Target = NormalizePowerPointSlideTarget(element.Attribute("Target")?.Value)
                    })
                    .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.Target))
                    .ToDictionary(item => item.Id!, item => item.Target!, StringComparer.OrdinalIgnoreCase))
                    ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                return presentationDocument
                    .Descendants(presentationNamespace + "sldId")
                    .Select(element => element.Attribute(officeDocumentRelationshipsNamespace + "id")?.Value)
                    .Where(relationshipId => !string.IsNullOrWhiteSpace(relationshipId))
                    .Select(relationshipId => slideTargetsByRelationshipId.GetValueOrDefault(relationshipId!))
                    .Where(target => !string.IsNullOrWhiteSpace(target))
                    .Cast<string>()
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Falling back to part-name slide order for PowerPoint extraction.");
                return new List<string>();
            }
        }

        private static string? NormalizePowerPointSlideTarget(string? target)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                return null;
            }

            var normalizedTarget = target.Replace('\\', '/').TrimStart('/');
            if (normalizedTarget.StartsWith("ppt/", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedTarget;
            }

            if (normalizedTarget.StartsWith("slides/", StringComparison.OrdinalIgnoreCase))
            {
                return $"ppt/{normalizedTarget}";
            }

            if (normalizedTarget.StartsWith("../", StringComparison.OrdinalIgnoreCase))
            {
                normalizedTarget = normalizedTarget.Substring(3);
            }

            return $"ppt/{normalizedTarget}";
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
