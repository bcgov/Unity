using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Worksheets;
using Unity.GrantManager.ApplicationForms;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Reporting.Configuration
{
    public class WorksheetsMetadataService(IWorksheetRepository worksheetRepository,
        IApplicationFormVersionAppService formVersionAppService)
        : IWorksheetsMetadataService, ITransientDependency
    {
        public async Task<WorksheetComponentMetaDataDto> GetWorksheetSchemaMetaDataAsync(Guid worksheetId, Guid formVersionId)
        {
            var worksheet = await worksheetRepository.GetAsync(worksheetId);
            var version = await formVersionAppService.GetAsync(formVersionId);            

            // Use the utility class to parse all fields in the worksheet
            var components = WorksheetFieldSchemaParser.ParseWorksheet(worksheet, version.FormSchema, version.SubmissionHeaderMapping);

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
