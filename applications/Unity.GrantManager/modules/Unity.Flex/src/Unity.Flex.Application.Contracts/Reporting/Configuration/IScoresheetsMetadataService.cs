using System;
using System.Threading.Tasks;

namespace Unity.Flex.Reporting.Configuration
{
    public interface IScoresheetsMetadataService
    {
        Task<ScoresheetComponentMetaDataDto> GetScoresheetSchemaMetaDataAsync(Guid scoresheetId);
        Task<ScoresheetComponentMetaDataDto> GetScoresheetSchemaMetaDataItemAsync(Guid scoresheetId, string fieldKey);
    }
}
