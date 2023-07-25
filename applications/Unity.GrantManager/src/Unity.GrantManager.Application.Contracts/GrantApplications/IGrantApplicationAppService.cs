using System.Collections.Generic;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications
{
    public interface IGrantApplicationAppService : IApplicationService
    {
        GrantApplicationsDto GetList();
    }
}
