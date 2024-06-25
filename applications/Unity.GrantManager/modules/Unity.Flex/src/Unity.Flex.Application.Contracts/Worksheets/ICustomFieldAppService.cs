using System;
using System.Threading.Tasks;

namespace Unity.Flex.Worksheets
{
    public interface ICustomFieldAppService
    {
        Task<CustomFieldDto> GetAsync(Guid id);
    }
}
