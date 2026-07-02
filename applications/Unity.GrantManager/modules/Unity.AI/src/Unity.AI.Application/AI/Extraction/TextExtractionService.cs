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
using System.Threading;
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

        public TextExtractionService(ILogger<TextExtractionService> logger)
        {
            _logger = logger;
        }

        public Task<string> ExtractTextAsync(string fileName, Stream fileContent, string contentType, CancellationToken cancellationToken = default)
        {
            if (fileContent == null)
            {
                return Task.FromResult(string.Empty);
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var normalizedContentType = contentType?.ToLowerInvariant() ?? string.Empty;
                var extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;

                if (extension == ".doc")
                {
                    return Task.FromResult(string.Empty);
                }

                var rawText = extension switch
                {
                    ".txt" or ".csv" or ".json" or ".xml" => ExtractTextFromTextFile(fileContent, cancellationToken),
                    ".pdf" => ExtractTextFromPdfFile(fileName, fileContent, cancellationToken),
                    ".docx" => ExtractTextFromWordDocx(fileName, fileContent, cancellationToken),
                    ".xls" or ".xlsx" => ExtractTextFromExcelFile(fileName, fileContent, cancellationToken),
                    ".pptx" => ExtractTextFromPowerPointFile(fileName, fileContent, cancellationToken),
                    _ => ExtractByContentType(fileName, fileContent, normalizedContentType, cancellationToken)
                };

                return Task.FromResult(NormalizeAndLimitText(rawText, fileName));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from {FileName}", fileName);
                return Task.FromResult(string.Empty);
            }
        }

        private string ExtractByContentType(
            string fileName,
            Stream fileContent,
            string normalizedContentType,
            CancellationToken cancellationToken)
        {
            if (normalizedContentType.Contains("text/"))
            {
                return ExtractTextFromTextFile(fileContent, cancellationToken);
            }
            if (normalizedContentType.Contains("pdf"))
            {
                return ExtractTextFromPdfFile(fileName, fileContent, cancellationToken);
            }
            if (normalizedContentType.Contains("word") ||
                normalizedContentType.Contains("msword") ||
                normalizedContentType.Contains("officedocument.wordprocessingml"))
            {
                return ExtractTextFromWordDocx(fileName, fileContent, cancellationToken);
            }
            if (normalizedContentType.Contains("excel") || normalizedContentType.Contains("spreadsheet"))
            {
                return ExtractTextFromExcelFile(fileName, fileContent, cancellationToken);
            }
            if (normalizedContentType.Contains("presentation") || normalizedContentType.Contains("powerpoint"))
            {
                return ExtractTextFromPowerPointFile(fileName, fileContent, cancellationToken);
            }
            return string.Empty;
        }

        private static void RewindIfPossible(Stream stream)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
        }

        private string ExtractTextFromTextFile(Stream fileContent, CancellationToken cancellationToken)
        {
            try
            {
                RewindIfPossible(fileContent);
                using var reader = new StreamReader(fileContent, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
                var buffer = new char[Math.Min(MaxExtractedTextLength, 8192)];
                var builder = new StringBuilder(capacity: Math.Min(MaxExtractedTextLength, 8192));
                int read;
                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var remaining = MaxExtractedTextLength - builder.Length;
                    if (remaining <= 0) break;
                    builder.Append(buffer, 0, Math.Min(read, remaining));
                    if (builder.Length >= MaxExtractedTextLength)
                    {
                        break;
                    }
                }
                return builder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding text file");
                return string.Empty;
            }
        }

        private string ExtractTextFromPdfFile(string fileName, Stream fileContent, CancellationToken cancellationToken)
        {
            try
            {
                RewindIfPossible(fileContent);
                using var document = PdfDocument.Open(fileContent);
                var builder = new StringBuilder();
                var processedPageCount = 0;
                var pageTexts = document.GetPages()
                    .Select(page => page.Text)
                    .Where(pageText => !string.IsNullOrWhiteSpace(pageText));

                foreach (var pageText in pageTexts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
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

                return builder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PDF text extraction failed for {FileName}", fileName);
                return string.Empty;
            }
        }

        private string ExtractTextFromWordDocx(string fileName, Stream fileContent, CancellationToken cancellationToken)
        {
            try
            {
                RewindIfPossible(fileContent);
                using var document = new XWPFDocument(fileContent);
                var builder = new StringBuilder();
                var processedParagraphCount = AppendDocxParagraphText(document, builder, cancellationToken);
                var processedTableRowCount = AppendDocxTableText(document, builder, cancellationToken);

                return builder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Word (.docx) text extraction failed for {FileName}", fileName);
                return string.Empty;
            }
        }

        private static int AppendDocxParagraphText(
            XWPFDocument document,
            StringBuilder builder,
            CancellationToken cancellationToken)
        {
            var processedParagraphCount = 0;
            var paragraphTexts = document.Paragraphs
                .Take(MaxDocxParagraphs)
                .Select(paragraph => paragraph.ParagraphText)
                .Where(paragraphText => !string.IsNullOrWhiteSpace(paragraphText));

            foreach (var paragraphText in paragraphTexts)
            {
                cancellationToken.ThrowIfCancellationRequested();
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

        private static int AppendDocxTableText(
            XWPFDocument document,
            StringBuilder builder,
            CancellationToken cancellationToken)
        {
            if (builder.Length >= MaxExtractedTextLength)
            {
                return 0;
            }

            var processedTableRowCount = 0;
            foreach (var table in document.Tables)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var row in table.Rows.Take(MaxDocxTableRows))
                {
                    cancellationToken.ThrowIfCancellationRequested();
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
                        cancellationToken.ThrowIfCancellationRequested();
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

        private string ExtractTextFromExcelFile(string fileName, Stream fileContent, CancellationToken cancellationToken)
        {
            try
            {
                RewindIfPossible(fileContent);
                using var workbook = WorkbookFactory.Create(fileContent);
                var builder = new StringBuilder();
                var sheetCount = Math.Min(workbook.NumberOfSheets, MaxExcelSheets);
                var processedSheetCount = 0;
                var processedRowCount = 0;

                for (var sheetIndex = 0; sheetIndex < sheetCount; sheetIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (builder.Length >= MaxExtractedTextLength)
                    {
                        break;
                    }

                    var sheet = workbook.GetSheetAt(sheetIndex);
                    var (rowsProcessed, limitReached) = TryAppendExcelSheet(sheet, builder, cancellationToken);
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

                return builder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Excel text extraction failed for {FileName}", fileName);
                return string.Empty;
            }
        }

        private string ExtractTextFromPowerPointFile(string fileName, Stream fileContent, CancellationToken cancellationToken)
        {
            try
            {
                RewindIfPossible(fileContent);
                using var archive = new ZipArchive(fileContent, ZipArchiveMode.Read, leaveOpen: true);
                var builder = new StringBuilder();
                var slideEntries = GetOrderedPowerPointSlideEntries(archive)
                    .Take(MaxPowerPointSlides);
                var processedSlideCount = 0;

                foreach (var slideEntry in slideEntries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
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
                return Enumerable.Empty<ZipArchiveEntry>();
            }

            var orderedSlideNames = TryGetPowerPointSlideOrder(archive);
            if (orderedSlideNames.Count == 0)
            {
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
            return orderedEntries;
        }

        private static (int RowsProcessed, bool LimitReached) TryAppendExcelSheet(
            ISheet? sheet,
            StringBuilder builder,
            CancellationToken cancellationToken)
        {
            if (sheet == null)
            {
                return (0, false);
            }

            var processedRows = 0;
            foreach (IRow row in sheet)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (processedRows >= MaxExcelRowsPerSheet || builder.Length >= MaxExtractedTextLength)
                {
                    break;
                }

                var (rowHadValue, limitReached) = TryAppendExcelRow(row, builder, cancellationToken);
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

        private static (bool RowHadValue, bool LimitReached) TryAppendExcelRow(
            IRow row,
            StringBuilder builder,
            CancellationToken cancellationToken)
        {
            var rowHasValue = false;
            foreach (var cell in row.Cells.Take(MaxExcelCellsPerRow))
            {
                cancellationToken.ThrowIfCancellationRequested();
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
            catch (Exception)
            {
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
