using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Reporting.Configuration;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.DependencyInjection;

namespace Unity.Reporting.Configuration.FieldsProviders
{
    /// <summary>
    /// Fields provider for consolidated form version submission views that span all form versions.
    /// Reads live field metadata directly from the form metadata service across all form versions,
    /// merges fields by (Label, Path, Type), and detects version changes for break notification.
    /// The CorrelationId for this provider is the FormId (not a specific form version ID).
    /// </summary>
    public class ConsolidatedFormVersionFieldsProvider(
        IApplicationFormAppService applicationFormAppService,
        IFormMetadataService formMetadataService)
        : IFieldsProvider, ITransientDependency
    {
        public string CorrelationProvider => Providers.FormVersionConsolidated;

        /// <summary>
        /// Retrieves and merges submission field metadata across all form versions for consolidated view configuration.
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
                var fullMetadata = await formMetadataService.GetFormComponentMetaDataAsync(version.Id);

                var fields = fullMetadata.Components
                    .Select(ConvertToFieldPathType)
                    .Where(x => x != null)
                    .Select(x => x!)
                    .ToArray();

                if (fields.Length == 0)
                    continue;

                versionsWithFields.Add((version.Id, versionLabel, fields));
                metadataInfo[$"formversion_{version.Id}"] = versionLabel;
            }

            var mergedFields = MergeFields(versionsWithFields);
            var mapMetadata = new MapMetadataDto { Info = metadataInfo };

            return new FieldPathMetaMapDto { Fields = [.. mergedFields], Metadata = mapMetadata };
        }

        /// <summary>
        /// Detects changes in form versions since the consolidated mapping was last saved.
        /// Returns a semicolon-joined change description or null if nothing has changed.
        /// Since form version fields are immutable, only added/removed versions are tracked.
        /// </summary>
        public async Task<string?> DetectChangesAsync(Guid formId, ReportColumnsMap reportColumnsMap)
        {
            var versions = await applicationFormAppService.GetVersionsAsync(formId);
            var currentInfo = new Dictionary<string, string>();

            foreach (var version in versions)
            {
                var versionLabel = $"v{version.Version}";
                var fullMetadata = await formMetadataService.GetFormComponentMetaDataAsync(version.Id);

                if (fullMetadata.Components.Count == 0)
                    continue;

                currentInfo[$"formversion_{version.Id}"] = versionLabel;
            }

            var storedInfo = GetStoredInfo(reportColumnsMap);
            var changes = DetectInfoChanges(storedInfo, currentInfo);

            return changes.Count > 0 ? string.Join("; ", changes) : null;
        }

        private static FieldPathTypeDto? ConvertToFieldPathType(FormComponentMetaDataItemDto? item)
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
            var exactMatchGroups = new Dictionary<string, List<(string VersionLabel, FieldPathTypeDto Field)>>(StringComparer.OrdinalIgnoreCase);
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
                    var versionsHavingThisExact = exactGroup.Select(e => e.VersionLabel).ToHashSet(StringComparer.OrdinalIgnoreCase);

                    if (typesForPath.Count > 1)
                    {
                        // Conflict: same (label, path) but different types — emit one row per
                        // distinct type, labelled with every version that carries that type.
                        // exactGroup already holds every (versionLabel, field) pair for this
                        // exact (label, path, type) triple, so join them all rather than using
                        // the outer-loop versionLabel (which would only reflect the first version
                        // encountered due to processedExactKeys suppressing subsequent entries).
                        result.Add(new FieldPathTypeDto
                        {
                            Id = field.Id,
                            Key = field.Key,
                            Label = field.Label,
                            Path = field.Path,
                            Type = field.Type,
                            TypePath = field.TypePath,
                            DataPath = field.DataPath,
                            VersionLabel = string.Join(", ", exactGroup.Select(e => e.VersionLabel))
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
                            VersionLabel = string.Join(", ", exactGroup.Select(e => e.VersionLabel))
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

            var addedVersionKeys = currentInfo.Keys
                .Where(k => k.StartsWith("formversion_", StringComparison.OrdinalIgnoreCase))
                .Except(storedInfo.Keys, StringComparer.OrdinalIgnoreCase);

            foreach (var key in addedVersionKeys)
            {
                var label = currentInfo[key];
                changes.Add($"Version added: {label}");
            }

            var removedVersionKeys = storedInfo.Keys
                .Where(k => k.StartsWith("formversion_", StringComparison.OrdinalIgnoreCase))
                .Except(currentInfo.Keys, StringComparer.OrdinalIgnoreCase);

            foreach (var key in removedVersionKeys)
            {
                var label = storedInfo[key];
                changes.Add($"Version removed: {label}");
            }

            return changes;
        }
    }
}
