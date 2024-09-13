using System;
using System.Threading.Tasks;

namespace Unity.Flex.Worksheets
{
    public interface ICustomFieldAppService
    {
        Task<CustomFieldDto> GetAsync(Guid id);
        Task<CustomFieldDto> EditAsync(Guid id, EditCustomFieldDto dto);
        Task DeleteAsync(Guid id);
    }
}
