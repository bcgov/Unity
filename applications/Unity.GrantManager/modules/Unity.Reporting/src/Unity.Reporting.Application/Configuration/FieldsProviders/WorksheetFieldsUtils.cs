using System.Collections.Generic;
using System.Linq;
using Unity.Reporting.Domain.Configuration;

namespace Unity.Reporting.Configuration.FieldsProviders
{
    /// <summary>
    /// Shared utilities for worksheet fields providers.
    /// </summary>
    internal static class WorksheetFieldsUtils
    {
        /// <summary>
        /// Prefixes duplicate DataPaths with (DK1), (DK2), … on both <see cref="FieldPathTypeDto.Path"/>
        /// and <see cref="FieldPathTypeDto.DataPath"/>, mirroring the behaviour of
        /// <c>FormMetadataService.UniqueifyPaths()</c> for worksheet fields.
        /// <para>
        /// Unlike the submissions path (where DataPath is derived from Path after uniqueification),
        /// worksheet DataPaths are constructed independently in the schema parser, so both properties
        /// must be prefixed here.
        /// </para>
        /// Mutates the array in place.
        /// </summary>
        /// <returns><c>true</c> if any duplicates were found and prefixed, otherwise <c>false</c>.</returns>
        internal static bool UniqueifyDataPaths(FieldPathTypeDto[] fields)
        {
            // Identify which DataPath values appear more than once
            var duplicatePaths = fields
                .Where(f => !string.IsNullOrEmpty(f.DataPath))
                .GroupBy(f => f.DataPath, System.StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet(System.StringComparer.OrdinalIgnoreCase);

            if (duplicatePaths.Count == 0) return false;

            var counters = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field.DataPath) || !duplicatePaths.Contains(field.DataPath))
                    continue;

                counters[field.DataPath] = counters.GetValueOrDefault(field.DataPath, 0) + 1;
                int n = counters[field.DataPath];

                field.Path = $"(DK{n}){field.Path}";
                field.DataPath = $"(DK{n}){field.DataPath}";
            }

            return true;
        }
    }
}
