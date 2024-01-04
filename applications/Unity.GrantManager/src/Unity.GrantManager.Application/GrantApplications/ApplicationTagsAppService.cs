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
        var tags  = await _applicationTagsRepository.GetListAsync();

        return ObjectMapper.Map<List<ApplicationTags>, List<ApplicationTagsDto>>(tags.OrderBy(t => t.Text).ToList());
    }

    public async Task<ApplicationTagsDto> CreateorUpdateTagsAsync(Guid id, ApplicationTagsDto input)
    {   

        var applicationTag = await _applicationTagsRepository.GetAsync(id);
        if (applicationTag != null)
        {
            applicationTag.Text = input.Text;
            
            await _applicationTagsRepository.UpdateAsync(applicationTag, autoSave: true);

            return ObjectMapper.Map<ApplicationTags, ApplicationTagsDto>(applicationTag);
        }
        else
        {
            var result = await _applicationTagsRepository.InsertAsync( new ApplicationTags
            {
                ApplicationId =  input.ApplicationId,
                Text = input.Text
            }, autoSave: true);

            return ObjectMapper.Map<ApplicationTags, ApplicationTagsDto>(result);
        }
       
    }
}