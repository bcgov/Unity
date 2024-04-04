using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.GrantApplications;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicationLinksAppService), typeof(IApplicationLinksService))]
public class ApplicationLinksAppService : CrudAppService<
            ApplicationLinks,
            ApplicationLinksDto,
            Guid>, IApplicationLinksService
{
    private readonly IApplicationLinksRepository _applicationLinksRepository;
    public ApplicationLinksAppService(IRepository<ApplicationLinks, Guid> repository,
        IApplicationLinksRepository applicationLinksRepository) : base(repository)
    {
        _applicationLinksRepository = applicationLinksRepository;
    }
    
    public async Task<List<ApplicationLinksDto>> GetListByApplicationAsync(Guid applicationId)
    {
        var links = await _applicationLinksRepository.GetListAsync(c => c.ApplicationId == applicationId);

        return ObjectMapper.Map<List<ApplicationLinks>, List<ApplicationLinksDto>>(links.ToList());
    }
}
