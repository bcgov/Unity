using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace Unity.GrantManager.GrantApplications;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicationStatusAppService), typeof(IApplicationStatusService))]
public class ApplicationStatusAppService : ApplicationService, IApplicationStatusService
{
    private readonly IApplicationStatusRepository _applicationStatusRepository;
    public ApplicationStatusAppService(IApplicationStatusRepository repository)
    {
        _applicationStatusRepository = repository;
    }

    public async Task<IList<ApplicationStatusDto>> GetListAsync()
    {        
        var statuses = await _applicationStatusRepository.GetListAsync();

        return ObjectMapper.Map<List<ApplicationStatus>, List<ApplicationStatusDto>>(statuses.OrderBy(s => s.StatusCode).ToList());
    }
}