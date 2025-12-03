using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Unity.Reporting.Domain.Configuration;

namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Utility class providing static methods for report mapping operations including field validation, 
    /// column name sanitization, uniqueness enforcement, and PostgreSQL compatibility checks.
    /// Ensures all generated column names conform to PostgreSQL naming restrictions and database best practices.
    /// </summary>
    internal static partial class ReportMappingUtils
    {
        private const int MaxColumnNameLength = 60;

        /// <summary>
        /// Generates sanitized and unique PostgreSQL-compatible column names from a dictionary of field keys and their display labels.
        /// Processes each label through sanitization to remove invalid characters, enforces uniqueness with numeric suffixes,
        /// and ensures all names comply with PostgreSQL identifier restrictions and length limits.
        /// </summary>
        /// <param name="keyColumns">Dictionary mapping field keys to their human-readable display labels used as the basis for column name generation.</param>
        /// <returns>Dictionary mapping the same field keys to their corresponding generated, sanitized, and unique column names suitable for PostgreSQL database use.</returns>
        internal static Dictionary<string, string> GenerateColumnNames(Dictionary<string, string> keyColumns)
        {
            var columnNames = new Dictionary<string, string>();
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var field in keyColumns)
            {
                var sanitizedName = SanitizeColumnName(field.Value);
                var uniqueName = EnsureUniqueness(sanitizedName, usedNames);

                columnNames[field.Key] = uniqueName;
                usedNames.Add(uniqueName);
            }

            return columnNames;
        }

        /// <summary>
        /// Sanitizes a raw input string to create a PostgreSQL-compliant column name by removing invalid characters,
        /// normalizing spacing and punctuation, ensuring proper length limits, and handling edge cases.
        /// Transforms the input to lowercase, replaces spaces and hyphens with underscores, removes consecutive underscores,
        /// ensures the name doesn't start with a digit, and truncates to the maximum allowed length while maintaining validity.
        /// </summary>
        /// <param name="input">The raw input string (typically a field label) to transform into a valid PostgreSQL column name.</param>
        /// <returns>A sanitized column name that is lowercase, contains only letters/numbers/underscores, doesn't start with a digit, 
        /// meets length requirements, and follows PostgreSQL identifier conventions. Returns "col_1" for null/empty input.</returns>
        private static string SanitizeColumnName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "col_1";

            // Remove invalid characters (keep only letters, numbers, and underscores)
            // Replace spaces and hyphens with underscores first
            string sanitized = input.Replace(" ", "_").Replace("-", "_");
            sanitized = ValidColumnNameRegex().Replace(sanitized, "");

            // Remove multiple consecutive underscores
            sanitized = MultipleUnderscoresRegex().Replace(sanitized, "_");

            // Trim underscores from start and end
            sanitized = sanitized.Trim('_');

            // Ensure we have something left
            if (string.IsNullOrEmpty(sanitized))
                sanitized = "col";

            // Ensure the column name doesn't start with a number
            if (char.IsDigit(sanitized[0]))
            {
                sanitized = "col_" + sanitized;
            }

            // Truncate to max length
            if (sanitized.Length > MaxColumnNameLength)
            {
                sanitized = sanitized[..MaxColumnNameLength];
                // Ensure we don't end with an underscore after truncation
                sanitized = sanitized.TrimEnd('_');
            }

            // Convert to lowercase for consistency
            return sanitized.ToLowerInvariant();
        }

        /// <summary>
        /// Ensures a column name is unique within a given set by appending incremental numeric suffixes when necessary.
        /// If the base name conflicts with existing names, appends "_1", "_2", etc., while respecting the maximum column length limit.
        /// Intelligently handles length constraints by truncating the base name when adding suffixes would exceed the limit,
        /// and falls back to generic naming patterns for extremely long suffixes.
        /// </summary>
        /// <param name="baseName">The base column name to make unique within the context of already used names.</param>
        /// <param name="usedNames">HashSet of already used column names (case-insensitive) that must be avoided to ensure uniqueness.</param>
        /// <returns>A unique column name that doesn't exist in the usedNames set, with numeric suffix appended if necessary, 
        /// and respecting PostgreSQL length limitations while maintaining readability.</returns>
        private static string EnsureUniqueness(string baseName, HashSet<string> usedNames)
        {
            var candidateName = baseName;
            var counter = 1;

            while (usedNames.Contains(candidateName))
            {
                var suffix = $"_{counter}";
                var maxBaseLength = MaxColumnNameLength - suffix.Length;

                if (maxBaseLength > 0)
                {
                    var trimmedBase = baseName.Length > maxBaseLength
                        ? baseName[..maxBaseLength].TrimEnd('_')
                        : baseName;
                    candidateName = trimmedBase + suffix;
                }
                else
                {
                    // If suffix is too long, just use a short name
                    candidateName = $"col_{counter}";
                }

                counter++;
            }

            return candidateName;
        }

        /// <summary>
        /// Compiled regular expression pattern for identifying and removing invalid characters from PostgreSQL column names.
        /// Matches any character that is not a letter (a-z, A-Z), digit (0-9), or underscore (_), which are the only
        /// characters allowed in PostgreSQL identifiers without requiring quotation.
        /// </summary>
        [GeneratedRegex(@"[^a-zA-Z0-9_]")]
        private static partial Regex ValidColumnNameRegex();

        /// <summary>
        /// Compiled regular expression pattern for consolidating multiple consecutive underscores into a single underscore.
        /// Matches sequences of two or more underscores and replaces them with a single underscore to improve
        /// readability and maintain clean column name formatting.
        /// </summary>
        [GeneratedRegex(@"_{2,}")]
        private static partial Regex MultipleUnderscoresRegex();

        /// <summary>
        /// Validates that all PropertyName values in the mapping rows are unique using case-insensitive comparison.
        /// PropertyNames represent the field keys/identifiers that should be unique within a single mapping configuration
        /// to avoid ambiguity and ensure proper field identification in the reporting system.
        /// </summary>
        /// <param name="rows">Array of mapping row DTOs to validate for PropertyName uniqueness.</param>
        /// <returns>True if all PropertyName values are unique (case-insensitive); false if duplicates are found. 
        /// Returns true for null or empty arrays as they are considered valid by default.</returns>
        internal static bool ValidateKeysUniqueness(UpsertMapRowDto[] rows)
        {
            if (rows == null || rows.Length == 0)
                return true;

            var propertyNames = rows.Select(r => r.PropertyName);
            var uniquePropertyNames = propertyNames.Distinct(StringComparer.OrdinalIgnoreCase);

            return propertyNames.Count() == uniquePropertyNames.Count();
        }

        /// <summary>
        /// Validates that all ColumnName values in the mapping rows are unique using case-insensitive comparison.
        /// ColumnNames represent the database column identifiers that must be unique within a table/view to prevent
        /// SQL conflicts and ensure proper data mapping in the generated reporting views.
        /// </summary>
        /// <param name="rows">Array of mapping row DTOs to validate for ColumnName uniqueness.</param>
        /// <returns>True if all ColumnName values are unique (case-insensitive); false if duplicates are found. 
        /// Returns true for null or empty arrays as they are considered valid by default.</returns>
        internal static bool ValidateColumnNamesUniqueness(UpsertMapRowDto[] rows)
        {
            if (rows == null || rows.Length == 0)
                return true;

            var columnNames = rows.Select(r => r.ColumnName);
            var uniqueColumnNames = columnNames.Distinct(StringComparer.OrdinalIgnoreCase);

            return columnNames.Count() == uniqueColumnNames.Count();
        }

        /// <summary>
        /// Validates that all Path values in the mapping rows are unique using case-insensitive comparison.
        /// Paths represent the hierarchical field paths (e.g., "panel1->field2") that should be unique within 
        /// a single correlation to ensure each field can be unambiguously identified and mapped correctly.
        /// </summary>
        /// <param name="rows">Array of mapping row DTOs to validate for Path uniqueness.</param>
        /// <returns>True if all Path values are unique (case-insensitive); false if duplicates are found. 
        /// Returns true for null or empty arrays as they are considered valid by default.</returns>
        internal static bool ValidatePathsUniqueness(UpsertMapRowDto[] rows)
        {
            if (rows == null || rows.Length == 0)
                return true;

            var paths = rows.Select(r => r.Path);
            var uniquePaths = paths.Distinct(StringComparer.OrdinalIgnoreCase);

            return paths.Count() == uniquePaths.Count();
        }

        /// <summary>
        /// Comprehensive collection of PostgreSQL reserved words that cannot be used as unquoted identifiers in SQL statements.
        /// These words have special meaning in PostgreSQL syntax and would cause parsing errors or unexpected behavior
        /// if used as column names without proper quotation. The collection includes keywords from SQL standards and 
        /// PostgreSQL-specific extensions, maintained as case-insensitive for robust validation.
        /// </summary>
        private static readonly HashSet<string> PostgreSqlReservedWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "all", "analyse", "analyze", "and", "any", "array", "as", "asc", "asymmetric",
            "authorization", "binary", "both", "case", "cast", "check", "collate", "collation",
            "column", "concurrently", "constraint", "create", "cross", "current_catalog",
            "current_date", "current_role", "current_schema", "current_time", "current_timestamp",
            "current_user", "default", "deferrable", "desc", "distinct", "do", "else", "end",
            "except", "false", "fetch", "for", "foreign", "freeze", "from", "full", "grant",
            "group", "having", "ilike", "in", "initially", "inner", "intersect", "into", "is",
            "isnull", "join", "lateral", "leading", "left", "like", "limit", "localtime",
            "localtimestamp", "natural", "not", "notnull", "null", "offset", "on", "only",
            "or", "order", "outer", "overlaps", "placing", "primary", "references", "returning",
            "right", "select", "session_user", "similar", "some", "symmetric", "table", "tablesample",
            "then", "to", "trailing", "true", "union", "unique", "user", "using", "variadic",
            "verbose", "when", "where", "window", "with"
        };

        /// <summary>
        /// Validates that all ColumnName values in the mapping rows conform to comprehensive PostgreSQL column name restrictions.
        /// Enforces multiple validation rules including: maximum length of 60 characters, prohibition against starting with digits,
        /// restriction to alphanumeric characters and underscores only, and prevention of PostgreSQL reserved word usage.
        /// Ensures all column names will be valid PostgreSQL identifiers that don't require quotation in SQL statements.
        /// </summary>
        /// <param name="rows">Array of mapping row DTOs to validate for PostgreSQL column name compliance.</param>
        /// <returns>True if all ColumnName values conform to PostgreSQL naming rules; false if any violations are detected. 
        /// Returns true for null or empty arrays. Empty/whitespace column names are allowed and skipped during validation.</returns>
        internal static bool ValidateColumnNamesConformance(UpsertMapRowDto[] rows)
        {
            if (rows == null || rows.Length == 0)
                return true;

            return rows
                .Where(row => !string.IsNullOrWhiteSpace(row.ColumnName))
                .Select(row => row.ColumnName.Trim())
                .All(columnName => 
                    columnName.Length <= MaxColumnNameLength &&
                    !char.IsDigit(columnName[0]) &&
                    IsValidColumnName(columnName) &&
                    !PostgreSqlReservedWords.Contains(columnName));
        }

        /// <summary>
        /// Performs character-level validation to ensure a column name contains only PostgreSQL-compliant characters.
        /// Validates that every character in the column name is either a letter (a-z, A-Z), digit (0-9), or underscore (_),
        /// which are the only characters allowed in PostgreSQL identifiers without requiring quotation marks.
        /// </summary>
        /// <param name="columnName">The column name string to validate for character compliance.</param>
        /// <returns>True if the column name contains only valid characters (letters, digits, underscores); false if any invalid characters are found.</returns>
        private static bool IsValidColumnName(string columnName)
        {
            // Column name should only contain letters, numbers, and underscores
            foreach (char c in columnName)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Creates a new ReportColumnsMap entity by intelligently merging user-provided column mappings with auto-generated mappings
        /// derived from field metadata. Prioritizes user-specified column names where provided, while automatically generating
        /// sanitized and unique column names for unmapped fields. Ensures all column names are PostgreSQL-compliant and unique
        /// across the entire mapping configuration.
        /// </summary>
        /// <param name="upsertReportColmnsMapDto">DTO containing correlation information and optional user-provided column mappings organized by field path.</param>
        /// <param name="fieldsMap">Tuple containing an array of field metadata (with keys, labels, types, paths) and additional mapping metadata from the correlation provider.</param>
        /// <returns>A new ReportColumnsMap entity with complete field mappings, serialized mapping configuration, and correlation details ready for database persistence.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when:
        /// - Fields array in fieldsMap is null or empty
        /// - CorrelationProvider in the DTO is null or empty
        /// </exception>
        internal static ReportColumnsMap CreateNewMap(UpsertReportColumnsMapDto upsertReportColmnsMapDto, FieldPathMetaMapDto fieldsMap)
        {
            if (fieldsMap.Fields == null || fieldsMap.Fields.Length == 0)
                throw new ArgumentException("Fields array cannot be null or empty", nameof(fieldsMap));

            if (string.IsNullOrWhiteSpace(upsertReportColmnsMapDto.CorrelationProvider))
                throw new ArgumentException("Correlation provider cannot be null or empty", nameof(upsertReportColmnsMapDto));

            // Create a dictionary of user-provided column name mappings (PropertyName -> ColumnName)
            var userProvidedMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (upsertReportColmnsMapDto.Mapping?.Rows != null)
            {
                userProvidedMappings = upsertReportColmnsMapDto.Mapping.Rows
                    .Where(row => !string.IsNullOrWhiteSpace(row.Path) && !string.IsNullOrWhiteSpace(row.ColumnName))
                    .ToDictionary(row => row.Path, row => row.ColumnName, StringComparer.OrdinalIgnoreCase);
            }

            // Track used column names to ensure uniqueness
            var usedColumnNames = new HashSet<string>(userProvidedMappings.Values, StringComparer.OrdinalIgnoreCase);
           
            // Generate column names for fields without user-provided mappings
            var autoGeneratedColumnNames = new Dictionary<string, string>();
            foreach (var field in fieldsMap.Fields)
            {
                if (!userProvidedMappings.ContainsKey(field.Path))
                {
                    var sanitizedName = SanitizeColumnName(field.Label ?? string.Empty);
                    var uniqueName = EnsureUniqueness(sanitizedName, usedColumnNames);
                    autoGeneratedColumnNames[field.Path] = uniqueName;
                    usedColumnNames.Add(uniqueName);
                }
            }

            // Create mapping rows
            var mapRows = fieldsMap.Fields.Select(field =>
            {
                // Use user-provided column name if available, otherwise use auto-generated
                var columnName = userProvidedMappings.TryGetValue(field.Path, out var userColumnName)
                    ? userColumnName
                    : autoGeneratedColumnNames[field.Path];

                return new MapRow
                {
                    Label = field.Label ?? string.Empty,
                    PropertyName = field.Key,
                    Type = field.Type,
                    ColumnName = columnName,
                    Path = field.Path,
                    DataPath = field.DataPath,
                    TypePath = field.TypePath,
                    Id = field.Id
                };
            }).ToList();

            // Create mapping object
            var mapping = new Mapping
            {
                Rows = [.. mapRows],
                Metadata = new MapMetadata() { Info = fieldsMap.Metadata?.Info }
            };

            // Create and return the map entity
            var map = new ReportColumnsMap
            {
                CorrelationId = upsertReportColmnsMapDto.CorrelationId,
                CorrelationProvider = upsertReportColmnsMapDto.CorrelationProvider,
                Mapping = JsonSerializer.Serialize(mapping)
            };

            return map;
        }

        /// <summary>
        /// Updates an existing ReportColumnsMap entity by intelligently merging current field metadata with existing mappings
        /// and user-provided updates. Implements a three-tier priority system: user-provided column names take precedence,
        /// followed by existing column names from the database, with auto-generated names for new fields. Maintains column
        /// name uniqueness across the entire mapping while preserving established mappings where possible.
        /// </summary>
        /// <param name="updateReportColumnsMapDto">DTO containing optional user-provided column mappings to update or add, organized by field path.</param>
        /// <param name="existing">The existing ReportColumnsMap entity from the database containing current mapping configuration and correlation details.</param>
        /// <param name="fieldsMap">Tuple containing current field metadata array (with keys, labels, types, paths) and additional mapping metadata from the correlation provider.</param>
        /// <returns>The updated ReportColumnsMap entity with merged mappings, updated serialized mapping configuration, and preserved correlation details.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the existing ReportColumnsMap entity is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the fields array in fieldsMap is null or empty.</exception>
        internal static ReportColumnsMap UpdateExistingMap(UpsertReportColumnsMapDto updateReportColumnsMapDto, ReportColumnsMap existing, FieldPathMetaMapDto fieldsMap)
        {
            ArgumentNullException.ThrowIfNull(existing);

            if (fieldsMap.Fields == null || fieldsMap.Fields.Length == 0)
                throw new ArgumentException("Fields array cannot be null or empty", nameof(fieldsMap));

            // Deserialize existing mappings
            var existingMapping = string.IsNullOrWhiteSpace(existing.Mapping)
                ? new Mapping()
                : JsonSerializer.Deserialize<Mapping>(existing.Mapping) ?? new Mapping();

            var existingMappings = existingMapping.Rows.ToDictionary(r => r.Path, StringComparer.OrdinalIgnoreCase);

            // Create a dictionary of user-provided column name mappings (Path -> ColumnName)
            var userProvidedMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (updateReportColumnsMapDto.Mapping?.Rows != null)
            {
                userProvidedMappings = updateReportColumnsMapDto.Mapping.Rows
                    .Where(row => !string.IsNullOrWhiteSpace(row.Path) && !string.IsNullOrWhiteSpace(row.ColumnName))
                    .ToDictionary(row => row.Path, row => row.ColumnName, StringComparer.OrdinalIgnoreCase);
            }

            // Track used column names to avoid duplicates
            var usedColumnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // First pass: add all user-provided column names to the used set
            usedColumnNames.UnionWith(userProvidedMappings.Values);

            // Second pass: add existing column names that won't be overwritten by user input
            usedColumnNames.UnionWith(fieldsMap.Fields
                .Where(field => !userProvidedMappings.ContainsKey(field.Path) && 
                               existingMappings.TryGetValue(field.Path, out var _))
                .Select(field => existingMappings[field.Path].ColumnName));

            var mapRows = fieldsMap.Fields.Select(field =>
            {
                string columnName;

                // Priority 1: User-provided column name in the update DTO
                if (userProvidedMappings.TryGetValue(field.Path, out var userColumnName))
                {
                    columnName = userColumnName;
                }
                // Priority 2: Existing column name from the database
                else if (existingMappings.TryGetValue(field.Path, out var existingRow))
                {
                    columnName = existingRow.ColumnName;
                }
                // Priority 3: Auto-generate for new fields
                else
                {
                    var sanitizedName = SanitizeColumnName(field.Label ?? string.Empty);
                    columnName = EnsureUniqueness(sanitizedName, usedColumnNames);
                    usedColumnNames.Add(columnName);
                }

                return new MapRow
                {
                    Label = field.Label ?? string.Empty,
                    PropertyName = field.Key,
                    Type = field.Type,
                    ColumnName = columnName,
                    Path = field.Path,
                    DataPath = field.DataPath,
                    TypePath = field.TypePath,
                    Id = field.Id
                };
            }).ToList();

            // Create new mapping object and serialize it
            var updatedMapping = new Mapping 
            { 
                Rows = [.. mapRows],
                Metadata = new MapMetadata() { Info = fieldsMap.Metadata?.Info }
            };
            existing.Mapping = JsonSerializer.Serialize(updatedMapping);

            return existing;
        }
    }
}
