using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.Reporting.Configuration
{
    public interface IFormMetadataService
    {
        Task<FormComponentMetaDataDto> GetFormComponentMetaDataAsync(Guid formVersionId);
        Task<FormComponentMetaDataDto> GetFormComponentMetaDataItemAsync(Guid formVersionId, string componentKey);
    }
}
