using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.Services;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.WorksheetInstances;
using Volo.Abp;

namespace Unity.Flex.Worksheets
{
    public class CustomFieldValueAppService(ICustomFieldValueRepository customFieldValueRepository,
        IWorksheetInstanceRepository worksheetInstanceRepository,
        WorksheetsManager worksheetsManager) : FlexAppService, ICustomFieldValueAppService
    {
        public async Task<CustomFieldValueDto> GetAsync(Guid id)
        {
            var field = await customFieldValueRepository.GetAsync(id);
            return ObjectMapper.Map<CustomFieldValue, CustomFieldValueDto>(field);
        }

        [RemoteService(false)]
        public async Task ExplicitSetAsync(Guid valueId, string value)
        {
            var field = await customFieldValueRepository.GetAsync(valueId);
            field.SetValue(value);
        }

        [RemoteService(false)]
        public async Task ExplicitAddAsync(CustomFieldValueDto value)
        {
            await customFieldValueRepository.InsertAsync(ObjectMapper.Map<CustomFieldValueDto, CustomFieldValue>(value));
        }

        [RemoteService(false)]
        public async Task SyncWorksheetInstanceValueAsync(Guid worksheetInstanceId)
        {
            var worksheetInstance = await worksheetInstanceRepository.GetAsync(worksheetInstanceId, true);
            
            // There may be a bug somewhere with ABP or EF, this magic line of code get the models to refresh and attach correctly to the aggregate
            _ = await customFieldValueRepository.GetListByWorksheetInstanceAsync(worksheetInstanceId);

            await worksheetsManager.UpdateWorksheetInstanceValueAsync(worksheetInstance);
        }
    }
}
