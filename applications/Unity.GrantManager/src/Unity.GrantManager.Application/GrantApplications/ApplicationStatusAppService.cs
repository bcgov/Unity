using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

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

    public virtual async Task<IList<ApplicationStatusDto>> GetListAsync()
    {        
        var statuses = await _applicationStatusRepository.GetListAsync();

        return ObjectMapper.Map<List<ApplicationStatus>, List<ApplicationStatusDto>>(statuses.OrderBy(s => s.StatusCode).ToList());
    }

    public virtual async Task UpdateExternalStatusLabelsAsync(UpdateApplicationStatusExternalLabelsDto input)
    {
        foreach (var statusDto in input.Statuses)
        {
            var status = await _applicationStatusRepository.GetAsync(statusDto.Id);
            status.ExternalStatus = statusDto.ExternalStatus;
            await _applicationStatusRepository.UpdateAsync(status);
        }
    }
}