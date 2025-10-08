using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Reporting.BackgroundJobs;
using Unity.Reporting.Configuration.FieldsProviders;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Service for managing report mappings between source fields and database columns for reporting.
    /// Provides operations for creating, updating, and retrieving column name mappings with automatic sanitization and validation.
    /// </summary>
    [Authorize]
    public class ReportMappingService(IReportColumnsMapRepository reportColumnsMapRepository,
        IEnumerable<IFieldsProvider> fieldsProviders,
        IBackgroundJobManager backgroundJobManager,
        ICurrentTenant currentTenant)
        : ReportingAppService, IReportMappingService
    {
        /// <summary>
        /// Creates a new report mapping for a specific correlation (worksheet, scoresheet, or form).
        /// Automatically generates sanitized and unique column names from field metadata.
        /// </summary>
        /// <param name="createReportColumnsMap">The report mapping creation request containing correlation details and mapping data.</param>
        /// <returns>The created report mapping with generated column names.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when:
        /// - A mapping with the same CorrelationId and CorrelationProvider already exists
        /// - Mapping contains duplicate key/path combinations within the same key
        /// - Mapping contains duplicate column names
        /// - Column names do not conform to PostgreSQL naming restrictions
        /// - Unknown or invalid correlation provider is specified
        /// </exception>
        public async Task<ReportColumnsMapDto> CreateAsync(UpsertReportColumnsMapDto createReportColumnsMap)
        {
            var existing = await reportColumnsMapRepository
                .FindByCorrelationAsync(createReportColumnsMap.CorrelationId, createReportColumnsMap.CorrelationProvider);

            if (existing != null)
            {
                throw new ArgumentException("A mapping with the same CorrelationId and CorrelationProvider already exists.");
            }

            FieldPathMetaMapDto fieldsMap = await GetFieldsMetadataAsync(createReportColumnsMap.CorrelationId, createReportColumnsMap.CorrelationProvider);

            ValidateMappingFields(createReportColumnsMap);

            ReportColumnsMap mapped = ReportMappingUtils.CreateNewMap(createReportColumnsMap, fieldsMap);

            var result = await reportColumnsMapRepository.InsertAsync(mapped);

            return ObjectMapper.Map<ReportColumnsMap, ReportColumnsMapDto>(result);
        }

        /// <summary>
        /// Updates an existing report mapping with new field metadata while intelligently merging existing and new field mappings.
        /// Retrieves current field metadata from the provider, validates the updated mapping data, and preserves existing column names 
        /// where possible while adding new fields with generated column names. User-provided mappings in the update DTO override existing mappings.
        /// </summary>
        /// <param name="updateReportColumnsMap">The report mapping update request containing correlation details and updated field mapping data.</param>
        /// <returns>The updated report mapping DTO with merged field mappings and preserved/generated column names.</returns>
        /// <exception cref="EntityNotFoundException">Thrown when no mapping exists for the specified CorrelationId and CorrelationProvider.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when:
        /// - Mapping contains duplicate key/path combinations within the same key
        /// - Mapping contains duplicate column names
        /// - Column names do not conform to PostgreSQL naming restrictions
        /// - Unknown or invalid correlation provider is specified
        /// </exception>
        public async Task<ReportColumnsMapDto> UpdateAsync(UpsertReportColumnsMapDto updateReportColumnsMap)
        {
            var existing = await reportColumnsMapRepository
               .FindByCorrelationAsync(updateReportColumnsMap.CorrelationId, updateReportColumnsMap.CorrelationProvider) ?? throw new EntityNotFoundException();

            var fieldsMap =
                await GetFieldsMetadataAsync(updateReportColumnsMap.CorrelationId, updateReportColumnsMap.CorrelationProvider);

            ValidateMappingFields(updateReportColumnsMap);

            ReportColumnsMap updated = ReportMappingUtils.UpdateExistingMap(updateReportColumnsMap, existing, fieldsMap);

            var result = await reportColumnsMapRepository.UpdateAsync(updated);

            return ObjectMapper.Map<ReportColumnsMap, ReportColumnsMapDto>(result);
        }

        /// <summary>
        /// Generates sanitized and unique column names from a dictionary mapping field keys to their display labels.
        /// Column names are transformed to conform to PostgreSQL naming restrictions by removing special characters,
        /// converting to lowercase, and ensuring uniqueness through numerical suffixes when necessary.
        /// </summary>
        /// <param name="keyColumns">Dictionary mapping field keys to their human-readable display labels.</param>
        /// <returns>Dictionary mapping the same field keys to their corresponding generated PostgreSQL-compatible column names.</returns>
        public Dictionary<string, string> GenerateColumnNames(Dictionary<string, string> keyColumns)
        {
            return ReportMappingUtils.GenerateColumnNames(keyColumns);
        }

        /// <summary>
        /// Retrieves an existing report mapping for a specific correlation (worksheet, scoresheet, or form).
        /// Validates the correlation provider, fetches the mapping from the database, and detects any schema changes
        /// by comparing current field metadata with the stored mapping configuration.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity (worksheet, scoresheet, or form ID).</param>
        /// <param name="correlationProvider">The provider type identifier (e.g., "worksheet", "scoresheet", "chefs").</param>
        /// <returns>The report mapping DTO with current mapping configuration and detected change information.</returns>
        /// <exception cref="ArgumentException">Thrown when an unknown or invalid correlation provider is specified.</exception>
        /// <exception cref="EntityNotFoundException">Thrown when no mapping exists for the specified CorrelationId and CorrelationProvider.</exception>
        public async Task<ReportColumnsMapDto> GetByCorrelationAsync(Guid correlationId, string correlationProvider)
        {
            var providerKey = correlationProvider?.ToLowerInvariant() ?? string.Empty;

            var result = ResolveFieldsProviders().TryGetValue(providerKey, out var provider);

            if (!result || correlationProvider == null || provider == null)
            {
                throw new ArgumentException($"Unknown correlation provider: {correlationProvider}");
            }

            var reportColumnsMap = await reportColumnsMapRepository.FindByCorrelationAsync(correlationId, correlationProvider);

            // If the ViewName is null, we can return a suggested View name

            var map = reportColumnsMap == null
                ? throw new EntityNotFoundException(typeof(ReportColumnsMap), $"CorrelationId: {correlationId}, CorrelationProvider: {correlationProvider}")
                : ObjectMapper.Map<ReportColumnsMap, ReportColumnsMapDto>(reportColumnsMap);


            // If we have an existing reportColumnsMap - let check if changes have occured that could effect the mapping
            map.DetectedChanges = await provider.DetectChangesAsync(correlationId, reportColumnsMap);

            return map;
        }

        /// <summary>
        /// Checks whether a report mapping exists for the specified correlation (worksheet, scoresheet, or form).
        /// Validates the correlation provider and queries the database for an existing mapping record.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity to check.</param>
        /// <param name="correlationProvider">The provider type identifier (e.g., "worksheet", "scoresheet", "chefs").</param>
        /// <returns>True if a mapping exists for the specified correlation; false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown when an unknown or invalid correlation provider is specified.</exception>
        public async Task<bool> ExistsAsync(Guid correlationId, string correlationProvider)
        {
            var providerKey = correlationProvider?.ToLowerInvariant() ?? string.Empty;
            var result = ResolveFieldsProviders().TryGetValue(providerKey, out var _);
            if (!result || correlationProvider == null)
            {
                throw new ArgumentException($"Unknown correlation provider: {correlationProvider}");
            }
            var existing = await reportColumnsMapRepository
               .FindByCorrelationAsync(correlationId, correlationProvider);

            return existing != null;
        }

        /// <summary>
        /// Retrieves comprehensive field metadata for all fields within a correlated entity (worksheet, scoresheet, or form).
        /// Delegates to the appropriate fields provider based on the correlation provider to extract field definitions,
        /// types, paths, labels, and additional metadata from the source schema.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity whose fields should be analyzed.</param>
        /// <param name="correlationProvider">The provider type identifier that determines which fields provider to use.</param>
        /// <returns>A tuple containing an array of field metadata DTOs and additional map metadata for all fields in the correlated entity.</returns>
        /// <exception cref="ArgumentException">Thrown when an unknown or invalid correlation provider is specified.</exception>
        public async Task<FieldPathMetaMapDto> GetFieldsMetadataAsync(Guid correlationId, string correlationProvider)
        {
            var providerKey = correlationProvider?.ToLowerInvariant() ?? string.Empty;

            if (ResolveFieldsProviders().TryGetValue(providerKey, out var provider))
            {
                return await provider.GetFieldsMetadataAsync(correlationId);
            }

            throw new ArgumentException($"Unknown correlation provider: {correlationProvider}");
        }

        /// <summary>
        /// Validates the integrity and format compliance of mapping fields in a report mapping request.
        /// Performs comprehensive validation including uniqueness of keys and paths, column name uniqueness,
        /// and PostgreSQL naming convention compliance. Logs warnings for certain validation failures.
        /// </summary>
        /// <param name="createReportColumnsMap">The report mapping request containing field mappings to validate.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when:
        /// - Mapping contains duplicate paths for the same key (combines key and path uniqueness failures)
        /// - Mapping contains duplicate column names across different fields
        /// - One or more column names do not conform to PostgreSQL naming restrictions
        /// </exception>
        /// <remarks>
        /// Individual key or path duplicates generate warnings but only throw exceptions when both occur simultaneously.
        /// </remarks>
        private void ValidateMappingFields(UpsertReportColumnsMapDto createReportColumnsMap)
        {
            var areKeysUnique = ReportMappingUtils.ValidateKeysUniqueness(createReportColumnsMap.Mapping.Rows);

            if (!areKeysUnique)
            {
                Logger.LogWarning("Mapping contains duplicate keys.");
            }

            var arePathsUnique = ReportMappingUtils.ValidatePathsUniqueness(createReportColumnsMap.Mapping.Rows);

            if (!arePathsUnique)
            {
                Logger.LogWarning("Mapping contains duplicate paths.");
            }

            if (!areKeysUnique && !arePathsUnique)
            {
                throw new ArgumentException("Mapping contains duplicate paths for the same key.");
            }

            var areColumnNamesUnique = ReportMappingUtils.ValidateColumnNamesUniqueness(createReportColumnsMap.Mapping.Rows);

            if (!areColumnNamesUnique)
            {
                throw new ArgumentException("Mapping contains duplicate column names.");
            }

            var areColumnsNameConforming = ReportMappingUtils.ValidateColumnNamesConformance(createReportColumnsMap.Mapping.Rows);

            if (!areColumnsNameConforming)
            {
                throw new ArgumentException("One or more column names do not conform to the required format.");
            }
        }

        /// <summary>
        /// Resolves and indexes all available field providers by their correlation provider keys for efficient lookup.
        /// Groups providers by their case-insensitive correlation provider names and returns the first provider
        /// in each group to handle duplicate registrations gracefully.
        /// </summary>
        /// <returns>A dictionary mapping lowercase provider keys to their corresponding IFieldsProvider instances for fast provider resolution.</returns>
        private Dictionary<string, IFieldsProvider> ResolveFieldsProviders()
        {
            return fieldsProviders
                .GroupBy(provider => provider.CorrelationProvider.ToLowerInvariant())
                .ToDictionary(
                    group => group.Key,
                    group => group.First()
                );
        }

        /// <summary>
        /// Checks if a view name is available for use in the database by verifying it doesn't already exist in the Reporting schema.
        /// Normalizes the view name to lowercase for consistent database naming and validates against existing view names.
        /// </summary>
        /// <param name="viewName">The proposed view name to check for availability in the database.</param>
        /// <returns>True if the view name is available (doesn't exist); false if the view already exists or the name is invalid.</returns>
        public async Task<bool> IsViewNameAvailableAsync(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                return false; // Empty or null view names are not available
            }

            // Normalize to lowercase for consistent handling
            var normalizedViewName = viewName.Trim().ToLowerInvariant();

            // Check if view already exists in the Reporting schema
            var viewExists = await reportColumnsMapRepository.ViewExistsAsync(normalizedViewName);

            return !viewExists; // Available if it doesn't exist
        }

        /// <summary>
        /// Checks if a view name is available for use by a specific correlation, allowing view name reuse within the same correlation.
        /// A view name is considered available if either no view with that name exists, or the existing view belongs to the same 
        /// correlation (enabling updates and regeneration of views for the same entity).
        /// </summary>
        /// <param name="viewName">The proposed view name to check for availability.</param>
        /// <param name="correlationId">The correlation ID of the entity requesting the view name.</param>
        /// <param name="correlationProvider">The correlation provider of the entity requesting the view name.</param>
        /// <returns>True if the view name is available for this correlation (either unused or owned by the same correlation); false if owned by a different correlation or name is invalid.</returns>
        public async Task<bool> IsViewNameAvailableAsync(string viewName, Guid correlationId, string correlationProvider)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                return false; // Empty or null view names are not available
            }

            // Normalize to lowercase for consistent handling
            var normalizedViewName = viewName.Trim().ToLowerInvariant();

            // First check if any ReportColumnsMap has this view name
            var existingMapping = await reportColumnsMapRepository.FindByViewNameAsync(normalizedViewName);

            if (existingMapping == null)
            {
                // No mapping uses this view name, so it's available
                return true;
            }

            // A mapping with this view name exists, check if it belongs to the same correlation
            return existingMapping.CorrelationId == correlationId &&
                   existingMapping.CorrelationProvider.Equals(correlationProvider, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initiates asynchronous generation of a database view based on an existing report mapping configuration.
        /// Validates the correlation provider and view name availability, updates the mapping with the new view name and status,
        /// and queues a background job to perform the actual view creation in the database. Handles view name changes by
        /// tracking the original view name for potential cleanup.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity whose mapping defines the view structure.</param>
        /// <param name="correlationProvider">The provider type identifier (e.g., "worksheet", "scoresheet", "chefs").</param>
        /// <param name="viewName">The desired name for the generated database view (will be normalized to lowercase).</param>
        /// <returns>A ViewGenerationResult indicating the view generation has been queued, including the normalized view name and original view name if changed.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when:
        /// - Unknown or invalid correlation provider is specified
        /// - View name is already in use by another reporting configuration
        /// </exception>
        /// <exception cref="EntityNotFoundException">Thrown when no mapping exists for the specified correlation.</exception>
        public async Task<ViewGenerationResult> GenerateViewAsync(Guid correlationId, string correlationProvider, string viewName)
        {
            var providerKey = correlationProvider?.ToLowerInvariant() ?? string.Empty;

            var result = ResolveFieldsProviders().TryGetValue(providerKey, out var _);

            if (!result || correlationProvider == null)
            {
                throw new ArgumentException($"Unknown correlation provider: {correlationProvider}");
            }

            // Normalize view name to lowercase for consistent handling
            var normalizedViewName = viewName.Trim().ToLowerInvariant();

            // Check if view name is available for this correlation
            if (!await IsViewNameAvailableAsync(normalizedViewName, correlationId, correlationProvider))
            {
                throw new ArgumentException($"View name '{normalizedViewName}' is already in use by another reporting configuration");
            }

            var reportColumnsMap = await reportColumnsMapRepository.FindByCorrelationAsync(correlationId, correlationProvider)
                ?? throw new EntityNotFoundException(typeof(ReportColumnsMap), $"CorrelationId: {correlationId}, CorrelationProvider: {correlationProvider}");

            var originalViewName = reportColumnsMap.ViewName;
            reportColumnsMap.ViewName = normalizedViewName;
            reportColumnsMap.ViewStatus = ViewStatus.GENERATING;

            await reportColumnsMapRepository.UpdateAsync(reportColumnsMap);

            // Queue background job to generate the view asynchronously
            await backgroundJobManager.EnqueueAsync(new GenerateViewBackgroundJobArgs
            {
                CorrelationId = correlationId,
                CorrelationProvider = correlationProvider,
                TenantId = currentTenant.Id,
                OriginalViewName = originalViewName
            },
            BackgroundJobPriority.Normal,
            TimeSpan.FromSeconds(1));

            return new ViewGenerationResult
            {
                Message = $"View '{normalizedViewName}' generation has been queued. The view will be created shortly.",
                ViewName = normalizedViewName,
                OriginalViewName = originalViewName,
                IsQueued = true
            };
        }

        /// <summary>
        /// Retrieves paginated and filtered data from a generated database view with support for sorting and custom filtering.
        /// Validates view existence, normalizes the view name, and delegates to the repository for secure data access
        /// with proper pagination controls to handle large datasets efficiently.
        /// </summary>
        /// <param name="viewName">The name of the database view to query for data.</param>
        /// <param name="request">The request parameters containing pagination settings (skip/take), filtering criteria, and sort ordering.</param>
        /// <returns>A ViewDataResult containing the queried data rows, total record count, and column information for the requested page.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when:
        /// - View name is null, empty, or whitespace
        /// - Specified view does not exist in the database
        /// </exception>
        public async Task<ViewDataResult> GetViewDataAsync(string viewName, ViewDataRequest request)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                throw new ArgumentException("View name cannot be null or empty", nameof(viewName));
            }

            // Normalize view name to lowercase
            var normalizedViewName = viewName.Trim().ToLowerInvariant();

            if (!await ViewExistsAsync(normalizedViewName))
            {
                throw new ArgumentException($"View '{normalizedViewName}' does not exist");
            }

            return await reportColumnsMapRepository.GetViewDataAsync(normalizedViewName, request);
        }

        /// <summary>
        /// Retrieves preview data from a generated database view showing only the top record based on ApplicationId sorting.
        /// Provides a quick sample of view data structure and content for preview purposes without loading full datasets.
        /// Validates view existence and normalizes the view name before querying.
        /// </summary>
        /// <param name="viewName">The name of the database view to query for preview data.</param>
        /// <param name="request">The request parameters for filtering (pagination settings are ignored as only top 1 record is returned).</param>
        /// <returns>A ViewDataResult containing the preview data (single top record), count of 1, and column information.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when:
        /// - View name is null, empty, or whitespace
        /// - Specified view does not exist in the database
        /// </exception>
        public async Task<ViewDataResult> GetViewPreviewDataAsync(string viewName, ViewDataRequest request)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                throw new ArgumentException("View name cannot be null or empty", nameof(viewName));
            }

            // Normalize view name to lowercase
            var normalizedViewName = viewName.Trim().ToLowerInvariant();

            if (!await ViewExistsAsync(normalizedViewName))
            {
                throw new ArgumentException($"View '{normalizedViewName}' does not exist");
            }

            return await reportColumnsMapRepository.GetViewPreviewDataAsync(normalizedViewName, request);
        }

        /// <summary>
        /// Retrieves the column names and structure information from a generated database view.
        /// Provides metadata about the view's schema without querying actual data, useful for
        /// building dynamic UI components, export functions, or data validation logic.
        /// </summary>
        /// <param name="viewName">The name of the database view to analyze for column information.</param>
        /// <returns>An array of column names in the order they appear in the view definition.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when:
        /// - View name is null, empty, or whitespace
        /// - Specified view does not exist in the database
        /// </exception>
        public async Task<string[]> GetViewColumnNamesAsync(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                throw new ArgumentException("View name cannot be null or empty", nameof(viewName));
            }

            // Normalize view name to lowercase
            var normalizedViewName = viewName.Trim().ToLowerInvariant();

            if (!await ViewExistsAsync(normalizedViewName))
            {
                throw new ArgumentException($"View '{normalizedViewName}' does not exist");
            }

            return await reportColumnsMapRepository.GetViewColumnNamesAsync(normalizedViewName);
        }

        /// <summary>
        /// Checks if a database view with the specified name exists in the Reporting schema.
        /// Normalizes the view name to lowercase for consistent database lookups and provides
        /// a quick existence check without retrieving view metadata or data.
        /// </summary>
        /// <param name="viewName">The name of the view to check for existence in the database.</param>
        /// <returns>True if the view exists in the Reporting schema; false if it doesn't exist or the name is invalid.</returns>
        public async Task<bool> ViewExistsAsync(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                return false;
            }

            // Normalize view name to lowercase
            var normalizedViewName = viewName.Trim().ToLowerInvariant();

            return await reportColumnsMapRepository.ViewExistsAsync(normalizedViewName);
        }

        /// <summary>
        /// Returns the current view name and status for a given correlation.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="correlationProvider"></param>
        /// <returns></returns>
        public async Task<ReportColumnsMapViewStatusDto> GetViewStatusByCorrlationAsync(Guid correlationId, string correlationProvider)
        {
            var reportColumnsMap = await reportColumnsMapRepository.FindByCorrelationAsync(correlationId, correlationProvider);
            if (reportColumnsMap == null)
            {
                return new ReportColumnsMapViewStatusDto();
            }

            return new ReportColumnsMapViewStatusDto
            {
                ViewName = reportColumnsMap.ViewName,
                ViewStatus = reportColumnsMap.ViewStatus
            };
        }

        /// <summary>
        /// Deletes a report mapping for a specific correlation and optionally deletes the associated database view.
        /// Provides comprehensive cleanup of both the mapping configuration and any generated database objects.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the correlated entity whose mapping should be deleted.</param>
        /// <param name="correlationProvider">The provider type identifier (e.g., "worksheet", "scoresheet", "chefs").</param>
        /// <param name="deleteView">Whether to also delete the associated database view if it exists. Defaults to true.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        /// <exception cref="ArgumentException">Thrown when an unknown or invalid correlation provider is specified.</exception>
        /// <exception cref="EntityNotFoundException">Thrown when no mapping exists for the specified correlation.</exception>
        public async Task DeleteAsync(Guid correlationId, string correlationProvider, bool deleteView = true)
        {
            var providerKey = correlationProvider?.ToLowerInvariant() ?? string.Empty;

            var result = ResolveFieldsProviders().TryGetValue(providerKey, out var _);

            if (!result || correlationProvider == null)
            {
                throw new ArgumentException($"Unknown correlation provider: {correlationProvider}");
            }

            var reportColumnsMap = await reportColumnsMapRepository.FindByCorrelationAsync(correlationId, correlationProvider)
                ?? throw new EntityNotFoundException(typeof(ReportColumnsMap), $"CorrelationId: {correlationId}, CorrelationProvider: {correlationProvider}");

            // Delete the associated view if requested and it exists
            if (deleteView && !string.IsNullOrWhiteSpace(reportColumnsMap.ViewName))
            {
                try
                {
                    var viewExists = await reportColumnsMapRepository.ViewExistsAsync(reportColumnsMap.ViewName);
                    if (viewExists)
                    {
                        await reportColumnsMapRepository.DeleteViewAsync(reportColumnsMap.ViewName);
                        Logger.LogInformation("Deleted database view: {ViewName}", reportColumnsMap.ViewName);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to delete database view: {ViewName}. Continuing with mapping deletion.", reportColumnsMap.ViewName);
                    // Continue with mapping deletion even if view deletion fails
                }
            }

            // Delete the mapping record
            await reportColumnsMapRepository.DeleteAsync(reportColumnsMap);
            
            Logger.LogInformation("Deleted report mapping for CorrelationId: {CorrelationId}, CorrelationProvider: {CorrelationProvider}", 
                correlationId, correlationProvider);
        }
    }
}
