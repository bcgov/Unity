using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Reporting.Configuration
{
    public class WorksheetsMetadataService(IWorksheetRepository worksheetRepository)
        : IWorksheetsMetadataService, ITransientDependency
    {
        public async Task<WorksheetComponentMetaDataDto> GetWorksheetSchemaMetaDataAsync(Guid worksheetId)
        {
            var worksheet = await worksheetRepository.GetAsync(worksheetId);
            
            // Use the utility class to parse all fields in the worksheet
            var components = WorksheetFieldSchemaParser.ParseWorksheet(worksheet);

            return new WorksheetComponentMetaDataDto()
            {
                Components = components
            };
        }

        public async Task<WorksheetComponentMetaDataDto> GetWorksheetSchemaMetaDataItemAsync(Guid worksheetId, string fieldKey)
        {
            var worksheet = await worksheetRepository.GetAsync(worksheetId);
            
            // Parse all components and find those matching the key
            var allComponents = WorksheetFieldSchemaParser.ParseWorksheet(worksheet);
            var matchingComponents = allComponents.Where(c => c.Key == fieldKey).ToList();

            return new WorksheetComponentMetaDataDto()
            {
                Components = matchingComponents
            };
        }
    }
}
