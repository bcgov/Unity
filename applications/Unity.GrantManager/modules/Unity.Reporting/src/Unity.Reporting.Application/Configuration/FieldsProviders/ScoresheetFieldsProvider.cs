using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Reporting.Configuration;
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
    public class ScoresheetFieldsProvider(IScoresheetsMetadataService scoresheetsMetadataService)
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
            var fullMetadata = await scoresheetsMetadataService.GetScoresheetSchemaMetaDataAsync(correlationId);

            FieldPathTypeDto[] convertedMetadata = [.. fullMetadata.Components
                .Select(ConvertToFieldPathType)
                .Where(x => x != null)
                .Select(x => x!)];

            return new FieldPathMetaMapDto() { Fields = convertedMetadata };
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

        public Task<string?> DetectChangesAsync(Guid correlationId, ReportColumnsMap reportColumnsMap)
        {
            throw new NotImplementedException();
        }
    }
}
