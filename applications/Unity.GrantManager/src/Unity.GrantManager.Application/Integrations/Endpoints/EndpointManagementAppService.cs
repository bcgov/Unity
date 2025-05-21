using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Integrations.Endpoints
{

    [ExposeServices(typeof(EndpointManagementAppService), typeof(IEndpointManagementAppService))]
    public class EndpointManagementAppService :
            CrudAppService<
            DynamicUrl,
            DynamicUrlDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateDynamicUrlDto>,
            IEndpointManagementAppService
    {
        public EndpointManagementAppService(IRepository<DynamicUrl, Guid> repository)
            : base(repository)
        {
        }
    }
}
