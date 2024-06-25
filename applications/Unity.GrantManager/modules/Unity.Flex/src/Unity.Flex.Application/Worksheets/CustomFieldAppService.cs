using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System;
using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Worksheets
{
    [Authorize]
    public class CustomFieldAppService(ICustomFieldRepository customFieldRepostitory) : FlexAppService, ICustomFieldAppService
    {
        public virtual async Task<CustomFieldDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<CustomField, CustomFieldDto>(await customFieldRepostitory.GetAsync(id, true));
        }
    }
}
