using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationStatusService : IApplicationService
{
    Task<PagedResultDto<ApplicationStatusDto>> GetListAsync();
}
