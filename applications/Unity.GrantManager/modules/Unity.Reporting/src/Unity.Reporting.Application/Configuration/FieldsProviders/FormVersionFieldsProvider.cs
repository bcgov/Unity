using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Reporting.Configuration;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.DependencyInjection;

namespace Unity.Reporting.Configuration.FieldsProviders
{
    public class FormVersionFieldsProvider(IFormMetadataService formMetadataService) : IFieldsProvider, ITransientDependency
    {
        public string CorrelationProvider => "formversion";

        /// <summary>
        /// Asynchronously retrieves metadata for fields associated with a specified correlation identifier.
        /// </summary>
        /// <remarks>This method filters and converts the retrieved metadata to the <see
        /// cref="FieldPathTypeDto"/> format.</remarks>
        /// <param name="correlationId">The unique identifier used to correlate the request for field metadata.</param>
        /// <returns>An array of <see cref="FieldPathTypeDto"/> objects representing the metadata of the fields. The array will
        /// be empty if no metadata is found for the given correlation identifier.</returns>
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
        /// Converts full FormFieldMetadataDto to minimal FieldPathTypeDto
        /// </summary>
        /// <param name="metadata">Full metadata object</param>
        /// <returns>Minimal metadata with only path and type</returns>
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
        /// Default to always return null as form versions are immutable
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="correlationProvider"></param>
        /// <returns></returns>
        public async Task<string?> DetectChangesAsync(Guid correlationId, ReportColumnsMap reportColumnsMap)
        {
            await Task.CompletedTask;
            return null;
        }
    }
}
