using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.WorksheetInstances;
using Volo.Abp;

namespace Unity.Flex.Worksheets
{
    public class CustomFieldValueAppService(ICustomFieldValueRepository customFieldValueRepository) : FlexAppService, ICustomFieldValueAppService
    {
        public async Task<CustomFieldValueDto> GetAsync(Guid id)
        {
            var field = await customFieldValueRepository.GetAsync(id);
            return ObjectMapper.Map<CustomFieldValue, CustomFieldValueDto>(field);
        }

        [RemoteService(false)]
        public async Task ExplicitSetAsync(Guid id, string value)
        {
            var field = await customFieldValueRepository.GetAsync(id);
            field.SetValue(value);
        }
    }
}
