using System;
using System.Threading.Tasks;

namespace Unity.Flex.Reporting.Configuration
{
    public interface IWorksheetsMetadataService
    {        
        Task<WorksheetComponentMetaDataDto> GetWorksheetSchemaMetaDataAsync(Guid worksheetId, Guid formVersionId);
        Task<WorksheetComponentMetaDataDto> GetWorksheetSchemaMetaDataItemAsync(Guid worksheetId, string fieldKey);
    }
}
