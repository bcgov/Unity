using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Reporting.Configuration;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.DependencyInjection;

namespace Unity.Reporting.Configuration.FieldsProviders
{
    /// <summary>
    /// Fields provider implementation for form versions that extracts field metadata from immutable form configurations.
    /// Retrieves component schemas from form versions through the GrantManager form metadata service,
    /// providing field definitions for forms that do not change over time. Since form versions are immutable,
    /// this provider always returns null for change detection as no structural changes can occur.
    /// </summary>
    public class FormVersionFieldsProvider(IFormMetadataService formMetadataService) : IFieldsProvider, ITransientDependency
    {
        /// <summary>
        /// Gets the correlation provider identifier for this fields provider.
        /// Identifies this provider as handling "formversion" correlation types in the reporting system.
        /// </summary>
        public string CorrelationProvider => "formversion";

        /// <summary>
        /// Asynchronously retrieves metadata for fields associated with a specified form version correlation identifier.
        /// Fetches component metadata from the form metadata service and converts it to the standard field path format
        /// used by the reporting system. Filters out null entries to ensure clean metadata arrays.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the form version whose field metadata should be retrieved.</param>
        /// <returns>A field path metadata map containing field definitions and their structural information for all fields in the form version.</returns>
        public async Task<FieldPathMetaMapDto> GetFieldsMetadataAsync(Guid correlationId)
        {
            var fullMetadata = await formMetadataService.GetFormComponentMetaDataAsync(correlationId);

            FieldPathTypeDto[] convertedMetadata = [.. fullMetadata.Components
                .Select(ConvertToFieldPathType)
                .Where(x => x != null)
                .Select(x => x!)];

            return new FieldPathMetaMapDto() { Fields = convertedMetadata };
        }

        /// <summary>
        /// Converts form component metadata to the standard field path type DTO format used by the reporting system.
        /// This conversion ensures compatibility between the form metadata format and the reporting field format,
        /// mapping all essential field properties for report generation and configuration.
        /// </summary>
        /// <param name="metadataItem">The form component metadata to convert, or null if no metadata is available.</param>
        /// <returns>Converted field path type DTO with all field properties mapped, or null if the input is null.</returns>
        private static FieldPathTypeDto? ConvertToFieldPathType(FormComponentMetaDataItemDto? metadataItem)
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
        /// Detects changes in form version configuration (always returns null for form versions).
        /// Since form versions are immutable by design, they never change after creation,
        /// eliminating the possibility of structural modifications that would affect reporting configurations.
        /// This method is implemented for interface compliance but will always return null.
        /// </summary>
        /// <param name="correlationId">The unique identifier of the form version (parameter unused but required by interface).</param>
        /// <param name="reportColumnsMap">The existing report columns mapping (parameter unused but required by interface).</param>
        /// <returns>Always returns null since form versions cannot change after creation.</returns>
        public async Task<string?> DetectChangesAsync(Guid correlationId, ReportColumnsMap reportColumnsMap)
        {
            await Task.CompletedTask;
            return null;
        }
    }
}
