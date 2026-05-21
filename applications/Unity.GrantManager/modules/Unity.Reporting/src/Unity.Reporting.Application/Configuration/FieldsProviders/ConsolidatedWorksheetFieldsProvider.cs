using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Reporting.Configuration;
using Unity.Flex.WorksheetLinks;
using Unity.GrantManager.ApplicationForms;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.DependencyInjection;

namespace Unity.Reporting.Configuration.FieldsProviders
{
    /// <summary>
    /// Fields provider for consolidated worksheet views that span all form versions.
    /// Reads live worksheet field metadata directly from the Flex module across all form versions,
    /// merges fields by (Label, Path, Type), and detects version/worksheet changes for break notification.
    /// The CorrelationId for this provider is the FormId (not a specific form version ID).
    /// </summary>
    public class ConsolidatedWorksheetFieldsProvider(
        IApplicationFormAppService applicationFormAppService,
        IWorksheetsMetadataService worksheetsMetadataService,
        IWorksheetLinkAppService worksheetLinkAppService)
        : IFieldsProvider, ITransientDependency
    {
        public string CorrelationProvider => Providers.WorksheetConsolidated;

        /// <summary>
        /// Retrieves and merges worksheet field metadata across all form versions for consolidated view configuration.
        /// Fields matching on (Label, Path, Type) are merged into a single column entry.
        /// Fields with the same (Label, Path) but different Type produce per-version conflict entries.
        /// Fields unique to one version are included with a VersionLabel marker.
        /// </summary>
        public async Task<FieldPathMetaMapDto> GetFieldsMetadataAsync(Guid formId)
        {
            var versions = await applicationFormAppService.GetVersionsAsync(formId);
            var versionsWithFields = new List<(Guid VersionId, string VersionLabel, FieldPathTypeDto[] Fields)>();
            var metadataInfo = new Dictionary<string, string>();

            foreach (var version in versions.OrderBy(v => v.Version))
            {
                var versionLabel = $"v{version.Version}";
                var links = await worksheetLinkAppService.GetListByCorrelationAsync(version.Id, "FormVersion");

                if (links.Count == 0)
                    continue;

                var allComponents = new List<FieldPathTypeDto>();

                foreach (var link in links)
                {
                    var metadata = await worksheetsMetadataService.GetWorksheetSchemaMetaDataAsync(link.WorksheetId, version.Id);
                    var components = metadata.Components
                        .Select(ConvertToFieldPathType)
                        .Where(x => x != null)
                        .Select(x => x!);
                    allComponents.AddRange(components);

                    var worksheetTitle = link.Worksheet?.Title ?? "Unknown Worksheet";
                    var worksheetName = link.Worksheet?.Name ?? "Unknown";
                    metadataInfo[$"ws_{version.Id}_{link.WorksheetId}"] = $"{worksheetTitle} ({worksheetName})";
                }

                versionsWithFields.Add((version.Id, versionLabel, [.. allComponents]));
                metadataInfo[$"formversion_{version.Id}"] = versionLabel;
            }

            var mergedFields = MergeFields(versionsWithFields);
            var mapMetadata = new MapMetadataDto { Info = metadataInfo };

            return new FieldPathMetaMapDto { Fields = [.. mergedFields], Metadata = mapMetadata };
        }

        /// <summary>
        /// Detects changes in form versions and worksheet links since the consolidated mapping was last saved.
        /// Returns a semicolon-joined change description or null if nothing has changed.
        /// </summary>
        public async Task<string?> DetectChangesAsync(Guid formId, ReportColumnsMap reportColumnsMap)
        {
            var versions = await applicationFormAppService.GetVersionsAsync(formId);
            var currentInfo = new Dictionary<string, string>();

            foreach (var version in versions)
            {
                var versionLabel = $"v{version.Version}";
                var links = await worksheetLinkAppService.GetListByCorrelationAsync(version.Id, "FormVersion");

                if (links.Count == 0)
                    continue;

                currentInfo[$"formversion_{version.Id}"] = versionLabel;

                foreach (var link in links)
                {
                    var worksheetTitle = link.Worksheet?.Title ?? "Unknown Worksheet";
                    var worksheetName = link.Worksheet?.Name ?? "Unknown";
                    currentInfo[$"ws_{version.Id}_{link.WorksheetId}"] = $"{worksheetTitle} ({worksheetName})";
                }
            }

            var storedInfo = GetStoredInfo(reportColumnsMap);
            var changes = DetectInfoChanges(storedInfo, currentInfo);

            return changes.Count > 0 ? string.Join("; ", changes) : null;
        }

        private static FieldPathTypeDto? ConvertToFieldPathType(WorksheetComponentMetaDataItemDto? item)
        {
            if (item == null)
                return null;

            return new FieldPathTypeDto
            {
                Id = item.Id,
                Path = item.Path,
                Type = item.Type,
                Key = item.Key,
                Label = item.Label,
                TypePath = item.TypePath,
                DataPath = item.DataPath
            };
        }

        private static List<FieldPathTypeDto> MergeFields(
            List<(Guid VersionId, string VersionLabel, FieldPathTypeDto[] Fields)> versionsWithFields)
        {
            // Track: (label.lower, path.lower, type.lower) → list of (versionLabel, field)
            var exactMatchGroups = new Dictionary<string, List<(string VersionLabel, FieldPathTypeDto Field)>>(StringComparer.OrdinalIgnoreCase);
            // Track: (label.lower, path.lower) → set of types seen
            var pathGroups = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var (_, versionLabel, fields) in versionsWithFields)
            {
                foreach (var field in fields)
                {
                    var exactKey = $"{field.Label?.ToLowerInvariant()}|{field.Path?.ToLowerInvariant()}|{field.Type?.ToLowerInvariant()}";
                    var pathKey = $"{field.Label?.ToLowerInvariant()}|{field.Path?.ToLowerInvariant()}";

                    if (!exactMatchGroups.TryGetValue(exactKey, out var exactList))
                    {
                        exactList = [];
                        exactMatchGroups[exactKey] = exactList;
                    }
                    // Only add first occurrence per version (avoid duplicates within same version)
                    if (!exactList.Any(e => e.VersionLabel == versionLabel))
                    {
                        exactList.Add((versionLabel, field));
                    }

                    if (!pathGroups.TryGetValue(pathKey, out var typeSet))
                    {
                        typeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        pathGroups[pathKey] = typeSet;
                    }
                    typeSet.Add(field.Type?.ToLowerInvariant() ?? string.Empty);
                }
            }

            var result = new List<FieldPathTypeDto>();
            var processedExactKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (_, versionLabel, fields) in versionsWithFields)
            {
                foreach (var field in fields)
                {
                    var exactKey = $"{field.Label?.ToLowerInvariant()}|{field.Path?.ToLowerInvariant()}|{field.Type?.ToLowerInvariant()}";
                    var pathKey = $"{field.Label?.ToLowerInvariant()}|{field.Path?.ToLowerInvariant()}";

                    if (processedExactKeys.Contains(exactKey))
                        continue;

                    processedExactKeys.Add(exactKey);

                    var typesForPath = pathGroups[pathKey];
                    var exactGroup = exactMatchGroups[exactKey];
                    var allVersionLabels = versionsWithFields.Select(v => v.VersionLabel).ToList();
                    var versionsHavingThisExact = exactGroup.Select(e => e.VersionLabel).ToHashSet(StringComparer.OrdinalIgnoreCase);

                    if (typesForPath.Count > 1)
                    {
                        // Conflict: same (label, path) but different types — emit per-version entry
                        result.Add(new FieldPathTypeDto
                        {
                            Id = field.Id,
                            Key = field.Key,
                            Label = field.Label,
                            Path = field.Path,
                            Type = field.Type,
                            TypePath = field.TypePath,
                            DataPath = field.DataPath,
                            VersionLabel = versionLabel
                        });
                    }
                    else if (versionsWithFields.Count > 1 && versionsHavingThisExact.Count == versionsWithFields.Count)
                    {
                        // Merged: exact match across all versions — no version label
                        result.Add(new FieldPathTypeDto
                        {
                            Id = field.Id,
                            Key = field.Key,
                            Label = field.Label,
                            Path = field.Path,
                            Type = field.Type,
                            TypePath = field.TypePath,
                            DataPath = field.DataPath,
                            VersionLabel = null
                        });
                    }
                    else
                    {
                        // Version-exclusive field: present in some but not all versions
                        result.Add(new FieldPathTypeDto
                        {
                            Id = field.Id,
                            Key = field.Key,
                            Label = field.Label,
                            Path = field.Path,
                            Type = field.Type,
                            TypePath = field.TypePath,
                            DataPath = field.DataPath,
                            VersionLabel = versionLabel
                        });
                    }
                }
            }

            return result;
        }

        private static Dictionary<string, string> GetStoredInfo(ReportColumnsMap reportColumnsMap)
        {
            if (string.IsNullOrEmpty(reportColumnsMap.Mapping))
                return [];

            try
            {
                var mapping = JsonSerializer.Deserialize<Mapping>(reportColumnsMap.Mapping);
                return mapping?.Metadata?.Info ?? [];
            }
            catch
            {
                return [];
            }
        }

        private static List<string> DetectInfoChanges(
            Dictionary<string, string> storedInfo,
            Dictionary<string, string> currentInfo)
        {
            var changes = new List<string>();

            // Detect added/removed form versions
            var addedVersionKeys = currentInfo.Keys
                .Where(k => k.StartsWith("formversion_", StringComparison.OrdinalIgnoreCase))
                .Except(storedInfo.Keys, StringComparer.OrdinalIgnoreCase);

            foreach (var key in addedVersionKeys)
            {
                var label = currentInfo[key];
                changes.Add($"Version added: {label} (has worksheets)");
            }

            var removedVersionKeys = storedInfo.Keys
                .Where(k => k.StartsWith("formversion_", StringComparison.OrdinalIgnoreCase))
                .Except(currentInfo.Keys, StringComparer.OrdinalIgnoreCase);

            foreach (var key in removedVersionKeys)
            {
                var label = storedInfo[key];
                changes.Add($"Version removed: {label}");
            }

            // Detect added/removed worksheets within versions
            var addedWsKeys = currentInfo.Keys
                .Where(k => k.StartsWith("ws_", StringComparison.OrdinalIgnoreCase))
                .Except(storedInfo.Keys, StringComparer.OrdinalIgnoreCase);

            foreach (var key in addedWsKeys)
            {
                var worksheetInfo = currentInfo[key];
                var versionLabel = GetVersionLabelFromWsKey(key, currentInfo);
                changes.Add($"Worksheet added to {versionLabel}: {worksheetInfo}");
            }

            var removedWsKeys = storedInfo.Keys
                .Where(k => k.StartsWith("ws_", StringComparison.OrdinalIgnoreCase))
                .Except(currentInfo.Keys, StringComparer.OrdinalIgnoreCase);

            foreach (var key in removedWsKeys)
            {
                var worksheetInfo = storedInfo[key];
                var versionLabel = GetVersionLabelFromWsKey(key, storedInfo);
                changes.Add($"Worksheet removed from {versionLabel}: {worksheetInfo}");
            }

            return changes;
        }

        // ws_{versionId}_{worksheetId} → look up formversion_{versionId} in info
        // versionId is a GUID (36 chars) starting at position 3 (after "ws_")
        private static string GetVersionLabelFromWsKey(string wsKey, Dictionary<string, string> info)
        {
            const int guidLength = 36;
            const int prefixLength = 3; // "ws_"

            if (wsKey.Length >= prefixLength + guidLength)
            {
                var versionIdStr = wsKey.Substring(prefixLength, guidLength);
                var versionKey = $"formversion_{versionIdStr}";
                if (info.TryGetValue(versionKey, out var label))
                    return label;
            }
            return "unknown version";
        }
    }
}
