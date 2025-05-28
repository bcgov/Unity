using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Unity.Flex.WorksheetInstances
{
    public interface ICustomFieldValueAppService : IApplicationService
    {
        Task<CustomFieldValueDto> GetAsync(Guid id);

        [RemoteService(false)]
        Task ExplicitSetAsync(Guid valueId, string value);

        [RemoteService(false)]
        Task ExplicitAddAsync(CustomFieldValueDto value);

        [RemoteService(false)]
        Task SyncWorksheetInstanceValueAsync(Guid worksheetInstanceId);
    }
}
