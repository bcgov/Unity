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
[ExposeServices(typeof(ApplicationTagsAppService), typeof(IApplicationTagsService))]
public class ApplicationTagsAppService : ApplicationService, IApplicationTagsService
{
    private readonly IApplicationTagsRepository _applicationTagsRepository;
    public ApplicationTagsAppService(IApplicationTagsRepository repository)
    {
        _applicationTagsRepository = repository;
    }

    public async Task<IList<ApplicationTagsDto>> GetListAsync()
    {
        var tags = await _applicationTagsRepository.GetListAsync();

        return ObjectMapper.Map<List<ApplicationTags>, List<ApplicationTagsDto>>(tags.OrderBy(t => t.Id).ToList());
    }

    public async Task<IList<ApplicationTagsDto>> GetListWithApplicationIdsAsync(List<Guid> ids)
    {
        var tags = await _applicationTagsRepository.GetListAsync(e => ids.Contains(e.ApplicationId));

        return ObjectMapper.Map<List<ApplicationTags>, List<ApplicationTagsDto>>(tags.OrderBy(t => t.Id).ToList());
    }

    public async Task<ApplicationTagsDto?> GetApplicationTagsAsync(Guid id)
    {
        var applicationTags = await _applicationTagsRepository.FirstOrDefaultAsync(s => s.ApplicationId == id);

        if (applicationTags == null) return null;

        return ObjectMapper.Map<ApplicationTags, ApplicationTagsDto>(applicationTags);
    }

    public async Task<ApplicationTagsDto> CreateorUpdateTagsAsync(Guid id, ApplicationTagsDto input)
    {
        var applicationTag = await _applicationTagsRepository.FirstOrDefaultAsync(e => e.ApplicationId == id);

        if (applicationTag == null)
        {
            var newTag = await _applicationTagsRepository.InsertAsync(new ApplicationTags
            {
                ApplicationId = input.ApplicationId,
                Text = input.Text

            }, autoSave: true);

            return ObjectMapper.Map<ApplicationTags, ApplicationTagsDto>(newTag);
        }
        else
        {
            applicationTag.Text = input.Text;
            await _applicationTagsRepository.UpdateAsync(applicationTag, autoSave: true);
            return ObjectMapper.Map<ApplicationTags, ApplicationTagsDto>(applicationTag);
        }
    }
}