using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Reporting.Configuration;
using Unity.GrantManager.ApplicationForms;
using Unity.Reporting.Configuration.FieldsProviders;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.DependencyInjection;

namespace Unity.Reporting.Configuration.FieldProviders
{
    /// <summary>
    /// Fields provider for scoresheet-based reporting. This provider handles the retrieval
    /// and conversion of scoresheet field metadata for use in reporting configurations.
    /// It supports complex field types that may contain multiple sub-components.
    /// </summary>
    public class ScoresheetFieldsProvider(IScoresheetsMetadataService scoresheetsMetadataService,
          IApplicationFormAppService applicationFormAppService)
        : IFieldsProvider, ITransientDependency
    {
        /// <summary>
        /// Gets the correlation provider identifier for this fields provider.
        /// Used to identify this provider in the reporting system.
        /// </summary>
        public string CorrelationProvider => "scoresheet";

        /// <summary>
        /// Retrieves metadata for all fields within a scoresheet.
        /// This includes both simple fields and expanded complex fields 
        /// (e.g., individual DataGrid columns, CheckboxGroup options, Radio options).
        /// </summary>
        /// <param name="correlationId">The unique identifier of the scoresheet</param>
        /// <returns>Array of all field metadata within the scoresheet</returns>
        public async Task<FieldPathMetaMapDto> GetFieldsMetadataAsync(Guid correlationId)
        {
            // For scoresheets, the correlationId is the formId
            var form = await applicationFormAppService.GetAsync(correlationId);

            var scoresheetId = form.ScoresheetId;

            // If the scoresheet ID is not set, return an empty metadata map
            if (scoresheetId == null)
            {
                return new FieldPathMetaMapDto() { Fields = [] };
            }

            var fullMetadata = await scoresheetsMetadataService.GetScoresheetSchemaMetaDataAsync(scoresheetId.Value);

            FieldPathTypeDto[] convertedMetadata = [.. fullMetadata.Components
                .Select(ConvertToFieldPathType)
                .Where(x => x != null)
                .Select(x => x!)];
            
            // Create metadata information about the scoresheet used
            var mapMetadata = new MapMetadataDto();
            var scoresheetKey = $"scoresheet_{scoresheetId.Value}";
            // Note: Since we only have the ID, we'll store just the ID for now
            // In a future enhancement, we might want to fetch the scoresheet name/title
            mapMetadata.Info[scoresheetKey] = $"Scoresheet ID: {scoresheetId.Value}";

            return new FieldPathMetaMapDto() { Fields = convertedMetadata, Metadata = mapMetadata };
        }

        /// <summary>
        /// Detects changes in the scoresheet configuration by comparing the current scoresheet
        /// with the scoresheet stored in the report columns mapping metadata.
        /// </summary>
        /// <param name="correlationId">The correlation identifier (form ID)</param>
        /// <param name="reportColumnsMap">The existing report columns mapping</param>
        /// <returns>A string describing detected changes, or null if no changes detected</returns>
        public async Task<string?> DetectChangesAsync(Guid correlationId, ReportColumnsMap reportColumnsMap)
        {
            // Get the current form configuration
            var form = await applicationFormAppService.GetAsync(correlationId);
            var currentScoresheetId = form.ScoresheetId;

            // Get current scoresheet info
            var currentScoresheetInfo = GetCurrentScoresheetInfo(currentScoresheetId);

            // Get stored metadata info from the mapping
            var storedScoresheetInfo = GetStoredScoresheetInfo(reportColumnsMap);

            // Compare current vs stored scoresheet info
            var changes = DetectScoresheetChanges(storedScoresheetInfo, currentScoresheetInfo);

            return changes.Any() ? string.Join("; ", changes) : null;
        }

        /// <summary>
        /// Converts scoresheet component metadata to the standard field path type DTO format
        /// used by the reporting system. This conversion ensures compatibility between
        /// the scoresheet metadata format and the reporting field format.
        /// </summary>
        /// <param name="metadataItem">The scoresheet component metadata to convert</param>
        /// <returns>Converted field path type DTO, or null if the input is null</returns>
        private static FieldPathTypeDto? ConvertToFieldPathType(ScoresheetComponentMetaDataItemDto? metadataItem)
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
        /// Get scoresheet info dictionary from current configuration
        /// </summary>
        /// <param name="scoresheetId">Current scoresheet ID (can be null)</param>
        /// <returns>Dictionary of scoresheet key to scoresheet info</returns>
        private static Dictionary<string, string> GetCurrentScoresheetInfo(Guid? scoresheetId)
        {
            var scoresheetInfo = new Dictionary<string, string>();

            if (scoresheetId.HasValue)
            {
                var scoresheetKey = $"scoresheet_{scoresheetId.Value}";
                scoresheetInfo[scoresheetKey] = $"Scoresheet ID: {scoresheetId.Value}";
            }

            return scoresheetInfo;
        }

        /// <summary>
        /// Extract stored scoresheet info from report columns mapping
        /// </summary>
        /// <param name="reportColumnsMap">Report columns mapping containing stored metadata</param>
        /// <returns>Dictionary of stored scoresheet info</returns>
        private static Dictionary<string, string> GetStoredScoresheetInfo(ReportColumnsMap reportColumnsMap)
        {
            var storedInfo = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(reportColumnsMap.Mapping))
                return storedInfo;

            try
            {
                // Parse the mapping JSON to extract metadata
                var mapping = JsonSerializer.Deserialize<Mapping>(reportColumnsMap.Mapping);
                var info = mapping?.Metadata?.Info;
                if (info != null)
                {
                    storedInfo = info;
                }
            }
            catch (JsonException)
            {
                // If JSON parsing fails, return empty info
                // This handles cases where the mapping format might be different
            }

            return storedInfo;
        }

        /// <summary>
        /// Detect changes between stored and current scoresheet info
        /// </summary>
        /// <param name="storedInfo">Previously stored scoresheet info</param>
        /// <param name="currentInfo">Current scoresheet info</param>
        /// <returns>List of change messages</returns>
        private static List<string> DetectScoresheetChanges(Dictionary<string, string> storedInfo, Dictionary<string, string> currentInfo)
        {
            var changes = new List<string>();

            // Find removed scoresheets
            var removedScoresheets = storedInfo.Keys.Except(currentInfo.Keys).ToList();
            foreach (var removedKey in removedScoresheets)
            {
                var scoresheetId = ExtractScoresheetId(storedInfo[removedKey]);
                changes.Add($"Scoresheet removed: {scoresheetId}");
            }

            // Find added scoresheets
            var addedScoresheets = currentInfo.Keys.Except(storedInfo.Keys).ToList();
            foreach (var addedKey in addedScoresheets)
            {
                var scoresheetId = ExtractScoresheetId(currentInfo[addedKey]);
                changes.Add($"Scoresheet added: {scoresheetId}");
            }

            // Check for scoresheet changes (if a scoresheet was replaced with another)
            var commonKeys = storedInfo.Keys.Intersect(currentInfo.Keys).ToList();
            foreach (var commonKey in commonKeys)
            {
                if (storedInfo[commonKey] != currentInfo[commonKey])
                {
                    var oldScoresheetId = ExtractScoresheetId(storedInfo[commonKey]);
                    var newScoresheetId = ExtractScoresheetId(currentInfo[commonKey]);
                    changes.Add($"Scoresheet changed from {oldScoresheetId} to {newScoresheetId}");
                }
            }

            return changes;
        }

        /// <summary>
        /// Extract scoresheet ID from the info string format "Scoresheet ID: {id}"
        /// </summary>
        /// <param name="infoString">The scoresheet info string</param>
        /// <returns>Extracted scoresheet ID or the original string if parsing fails</returns>
        private static string ExtractScoresheetId(string infoString)
        {
            if (string.IsNullOrEmpty(infoString))
                return "Unknown";

            // Try to extract the ID part after "Scoresheet ID: "
            const string prefix = "Scoresheet ID: ";
            if (infoString.StartsWith(prefix))
            {
                return infoString.Substring(prefix.Length);
            }

            return infoString;
        }
    }

    /// <summary>
    /// Simplified mapping class to match the structure expected by JSON deserialization
    /// </summary>
    internal class Mapping
    {
        public MapMetadataDto? Metadata { get; set; }
    }
}
