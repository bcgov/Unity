using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.Modules.Shared;
using Volo.Abp.Application.Dtos;
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

    public async Task<List<ApplicationTagsDto>> GetListWithApplicationIdsAsync(List<Guid> ids)
    {
        var queryable = await _applicationTagsRepository.GetQueryableAsync();

        var tags = await queryable
            .Where(x => ids.Contains(x.ApplicationId))
            .Include(x => x.Tag)
            .ToListAsync();

        return ObjectMapper.Map<List<ApplicationTags>, List<ApplicationTagsDto>>(tags.OrderBy(t => t.Id).ToList());
    }

    public async Task<List<ApplicationTagsDto>> GetApplicationTagsAsync(Guid id)
    {
        var tags = await (await _applicationTagsRepository
                 .WithDetailsAsync(x => x.Tag))
             .Where(e => e.ApplicationId == id)
             .ToListAsync();

        return ObjectMapper.Map<List<ApplicationTags>, List<ApplicationTagsDto>>(tags);
    }

    public async Task<List<ApplicationTagsDto>> AssignTagsAsync(AssignApplicationTagsDto input)
    {
        var existingApplicationTags = await _applicationTagsRepository.GetListAsync(e => e.ApplicationId == input.ApplicationId);
        var existingTagIds = existingApplicationTags.Select(t => t.TagId).ToHashSet();
        var inputTagIds = input.Tags?.Select(t => t.Id).ToHashSet() ?? new HashSet<Guid>();

        var newTagsToAdd = input.Tags?
            .Where(tag => !existingTagIds.Contains(tag.Id))
            .Select(tag => new ApplicationTags
            {
                ApplicationId = input.ApplicationId,
                TagId = tag.Id
            })
            .ToList();

        var tagsToRemove = existingApplicationTags
           .Where(et => !inputTagIds.Contains(et.TagId))
           .ToList();

        if (tagsToRemove.Count > 0 && await AuthorizationService.IsGrantedAsync(UnitySelector.Application.Tags.Delete))
        {
            await _applicationTagsRepository.DeleteManyAsync(tagsToRemove, autoSave: true);
        }

        if (newTagsToAdd?.Count > 0 && await AuthorizationService.IsGrantedAsync(UnitySelector.Application.Tags.Create))
        {
            await _applicationTagsRepository.InsertManyAsync(newTagsToAdd, autoSave: true);

            var tagIds = newTagsToAdd.Select(x => x.TagId).ToList();

            var insertedTagsWithNavProps = await (await _applicationTagsRepository.GetQueryableAsync())
                .Where(x => x.ApplicationId == input.ApplicationId && tagIds.Contains(x.TagId))
                .Include(x => x.Tag)
                .ToListAsync();

            return ObjectMapper.Map<List<ApplicationTags>, List<ApplicationTagsDto>>(insertedTagsWithNavProps);
        }
        else
        {
            return [];
        }
    }

    [Authorize(UnitySelector.SettingManagement.Tags.Default)]
    public async Task<PagedResultDto<TagSummaryCountDto>> GetTagSummaryAsync()
    {
        var tagSummary = ObjectMapper.Map<List<TagSummaryCount>, List<TagSummaryCountDto>>(
            await _applicationTagsRepository.GetTagSummary());

        return new PagedResultDto<TagSummaryCountDto>(
            tagSummary.Count,
            tagSummary
        );
    }

    /// <summary>
    /// Deletes a tag from all application tags. Only whole-word tags are removed; substring matches are ignored.
    /// </summary>
    /// <param name="deleteTag">String of tag to be deleted.</param>
    [Authorize(UnitySelector.SettingManagement.Tags.Delete)]
    public async Task DeleteTagWithTagIdAsync(Guid id)
    {

        var existingApplicationTags = await _applicationTagsRepository.GetListAsync(e => e.Tag.Id == id);
        var idsToDelete = existingApplicationTags.Select(x => x.Id).ToList();
        await _applicationTagsRepository.DeleteManyAsync(idsToDelete, autoSave: true);



    }

    [Authorize(UnitySelector.SettingManagement.Tags.Delete)]
    public async Task DeleteTagAsync(Guid id)
    {

        await _applicationTagsRepository.DeleteAsync(id);
    }

}