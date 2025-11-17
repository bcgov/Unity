using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Reporting.Configuration;
using Unity.Flex.WorksheetLinks;
using Unity.Reporting.Configuration.FieldsProviders;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.DependencyInjection;
using System.Text.Json;

namespace Unity.Reporting.Configuration.FieldProviders
{
    /// <summary>
    /// Fields provider implementation for Unity.Flex worksheets that extracts field metadata from linked worksheets.
    /// Discovers worksheet links associated with form versions, retrieves component schemas from the Flex module,
    /// and provides comprehensive field analysis with change detection for worksheet-based reporting configurations.
    /// Handles worksheet additions/removals and provides detailed change tracking for dynamic report mapping.
    /// </summary>
    public class WorksheetFieldsProvider(IWorksheetsMetadataService worksheetsMetadataService,
        IWorksheetLinkAppService worksheetLinkAppService)
        : IFieldsProvider, ITransientDependency
    {
        public string CorrelationProvider => "worksheet";

        /// <summary>
        /// Get the fields metadata for the given correlation id for a worksheet from the flex module
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        public async Task<FieldPathMetaMapDto> GetFieldsMetadataAsync(Guid correlationId)
        {
            // Make sure the Flex module is enabled for the tenant up to this point            
            var links = await worksheetLinkAppService.GetListByCorrelationAsync(correlationId, "FormVersion");
            List<WorksheetComponentMetaDataDto> worksheetMetadata = [];
            var mapMetadata = new MapMetadataDto();

            foreach (var link in links)
            {
                var metadata = await worksheetsMetadataService.GetWorksheetSchemaMetaDataAsync(link.WorksheetId);
                worksheetMetadata.Add(metadata);

                // Add worksheet information to the metadata map
                var worksheetKey = $"worksheet_{link.WorksheetId}";
                var worksheetName = link.Worksheet?.Name ?? "Unknown";
                var worksheetTitle = link.Worksheet?.Title ?? "Unknown Worksheet";
                mapMetadata.Info[worksheetKey] = $"{worksheetTitle} ({worksheetName}) - ID: {link.WorksheetId}";
            }

            FieldPathTypeDto[] convertedMetadata = [.. worksheetMetadata.SelectMany(s => s.Components)
                .Select(ConvertToFieldPathType)
                .Where(x => x != null)
                .Select(x => x!)];

            return new FieldPathMetaMapDto() { Fields = convertedMetadata, Metadata = mapMetadata };
        }

        /// <summary>
        /// Converts full FieldPathTypeDto to minimal FieldPathTypeDto
        /// </summary>
        /// <param name="metadata">Full metadata object</param>
        /// <returns>Minimal metadata with only path and type</returns>
        private static FieldPathTypeDto? ConvertToFieldPathType(WorksheetComponentMetaDataItemDto? metadataItem)
        {
            if (metadataItem == null)
                return null;

            return new FieldPathTypeDto
            {
                Id = metadataItem.Id,
                Path = metadataItem.Path,
                Type = metadataItem.Type,
                Key = metadataItem.Key,
                Label = metadataItem.Label,
                TypePath = metadataItem.TypePath,
                DataPath = metadataItem.DataPath
            };
        }

        /// <summary>
        /// Return appropriate change detection for worksheets
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="reportColumnsMap"></param>
        /// <returns></returns>
        public async Task<string?> DetectChangesAsync(Guid correlationId, ReportColumnsMap reportColumnsMap)
        {
            var currentLinks = await worksheetLinkAppService.GetListByCorrelationAsync(correlationId, "FormVersion");
            var currentWorksheetInfo = GetWorksheetInfoFromLinks(currentLinks);

            // Get stored metadata info from the mapping
            var storedWorksheetInfo = GetStoredWorksheetInfo(reportColumnsMap);

            // Compare current vs stored worksheet info
            var changes = DetectWorksheetChanges(storedWorksheetInfo, currentWorksheetInfo);

            return changes.Any() ? string.Join("; ", changes) : null;
        }

        /// <summary>
        /// Get worksheet info dictionary from current links
        /// </summary>
        /// <param name="links">Current worksheet links</param>
        /// <returns>Dictionary of worksheet key to worksheet info</returns>
        private static Dictionary<string, string> GetWorksheetInfoFromLinks(List<WorksheetLinkDto> links)
        {
            var worksheetInfo = new Dictionary<string, string>();

            foreach (var link in links)
            {
                var worksheetKey = $"worksheet_{link.WorksheetId}";
                var worksheetName = link.Worksheet?.Name ?? "Unknown";
                var worksheetTitle = link.Worksheet?.Title ?? "Unknown Worksheet";
                worksheetInfo[worksheetKey] = $"{worksheetTitle} ({worksheetName}) - ID: {link.WorksheetId}";
            }

            return worksheetInfo;
        }

        /// <summary>
        /// Extract stored worksheet info from report columns mapping
        /// </summary>
        /// <param name="reportColumnsMap">Report columns mapping containing stored metadata</param>
        /// <returns>Dictionary of stored worksheet info</returns>
        private static Dictionary<string, string> GetStoredWorksheetInfo(ReportColumnsMap reportColumnsMap)
        {
            var storedInfo = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(reportColumnsMap.Mapping))
                return storedInfo;

            var mapping = JsonSerializer.Deserialize<Mapping>(reportColumnsMap.Mapping);
            var info = mapping?.Metadata?.Info;
            if (info != null)
            {
                storedInfo = info;
            }

            return storedInfo;
        }

        /// <summary>
        /// Detect changes between stored and current worksheet info
        /// </summary>
        /// <param name="storedInfo">Previously stored worksheet info</param>
        /// <param name="currentInfo">Current worksheet info</param>
        /// <returns>List of change messages</returns>
        private static List<string> DetectWorksheetChanges(Dictionary<string, string> storedInfo, Dictionary<string, string> currentInfo)
        {
            var changes = new List<string>();

            // Find removed worksheets
            var removedWorksheets = storedInfo.Keys.Except(currentInfo.Keys).ToList();
            foreach (var removedKey in removedWorksheets)
            {
                var worksheetName = ExtractWorksheetName(storedInfo[removedKey]);
                changes.Add($"Worksheet removed: {worksheetName}");
            }

            // Find added worksheets
            var addedWorksheets = currentInfo.Keys.Except(storedInfo.Keys).ToList();
            foreach (var addedKey in addedWorksheets)
            {
                var worksheetName = ExtractWorksheetName(currentInfo[addedKey]);
                changes.Add($"Worksheet added: {worksheetName}");
            }

            return changes;
        }

        /// <summary>
        /// Extract worksheet name from the info string format "Title (Name) - ID: {id}"
        /// </summary>
        /// <param name="infoString">The worksheet info string</param>
        /// <returns>Extracted worksheet name or the original string if parsing fails</returns>
        private static string ExtractWorksheetName(string infoString)
        {
            if (string.IsNullOrEmpty(infoString))
                return "Unknown";

            // Try to extract the title part before the first opening parenthesis
            var titleEndIndex = infoString.IndexOf('(');
            if (titleEndIndex > 0)
            {
                return infoString.Substring(0, titleEndIndex).Trim();
            }

            return infoString;
        }
    }
}
