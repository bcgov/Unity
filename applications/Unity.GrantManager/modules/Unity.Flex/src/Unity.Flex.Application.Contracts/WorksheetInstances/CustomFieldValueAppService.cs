using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.WorksheetInstances
{
    public interface ICustomFieldValueAppService : IApplicationService
    {
        Task<CustomFieldValueDto> GetAsync(Guid id);

        Task ExplicitSetAsync(Guid id, string value);
    }
}
