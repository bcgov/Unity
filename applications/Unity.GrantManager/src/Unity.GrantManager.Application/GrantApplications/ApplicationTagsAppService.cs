using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.ObjectMapping;

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

    public async Task<ApplicationTagsDto> GetApplicationTagsAsync(Guid id)
    {
        var tag  = await _applicationTagsRepository.GetAsync(e => id == e.ApplicationId);

        return ObjectMapper.Map<ApplicationTags, ApplicationTagsDto>(tag);
    }

    public async Task<ApplicationTagsDto> CreateorUpdateTagsAsync(Guid id, ApplicationTagsDto input)
    {
        try
        {
            var applicationTag = await _applicationTagsRepository.GetAsync(e => e.ApplicationId == id);

            applicationTag.Text = input.Text;

            await _applicationTagsRepository.UpdateAsync(applicationTag, autoSave: true);

            return ObjectMapper.Map<ApplicationTags, ApplicationTagsDto>(applicationTag);
        }
        catch (EntityNotFoundException ex)
        {
            var result = await _applicationTagsRepository.InsertAsync(new ApplicationTags
            {
                ApplicationId = input.ApplicationId,
                Text = input.Text
            }, autoSave: true);

            return ObjectMapper.Map<ApplicationTags, ApplicationTagsDto>(result);
        }


    }
}