using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.Modules.Shared;
using Unity.Payments.Events;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp.ObjectMapping;

namespace Unity.GrantManager.GrantApplications;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicationTagsAppService), typeof(IApplicationTagsService))]
public class ApplicationTagsAppService : ApplicationService, IApplicationTagsService
{
    private readonly IApplicationTagsRepository _applicationTagsRepository;
    private readonly ILocalEventBus _localEventBus;

    public ApplicationTagsAppService(IApplicationTagsRepository repository, ILocalEventBus localEventBus)
    {
        _applicationTagsRepository = repository;
        _localEventBus = localEventBus;
    }

    public async Task<IList<ApplicationTagsDto>> GetListAsync()
    {
        var tags = await _applicationTagsRepository.GetListAsync();

        return ObjectMapper.Map<List<ApplicationTags>, List<ApplicationTagsDto>>(tags.OrderBy(t => t.Id).ToList());
    }

    public async Task<List<ApplicationTagsDto>> GetListWithApplicationIdsAsync(List<Guid> ids)
    {
        var tags = await _applicationTagsRepository.GetListAsync(e => ids.Contains(e.ApplicationId));

        return ObjectMapper.Map<List<ApplicationTags>, List<ApplicationTagsDto>>(tags.OrderBy(t => t.Id).ToList());
    }

    public async Task<ApplicationTagsDto?> GetApplicationTagsAsync(Guid id)
    {
        var applicationTags = await (await _applicationTagsRepository.GetQueryableAsync())
        .Include(x => x.Tag)
        .FirstOrDefaultAsync(x => x.ApplicationId == id);

        if (applicationTags == null) return null;

        return ObjectMapper.Map<ApplicationTags, ApplicationTagsDto>(applicationTags);
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


        if (tagsToRemove.Any())
        {
            await _applicationTagsRepository.DeleteManyAsync(tagsToRemove, autoSave: true);
        }
        if (newTagsToAdd?.Count > 0)
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
            return new List<ApplicationTagsDto>();
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
    /// For a given Tag, finds the maximum length available for renaming.
    /// </summary>
    /// <param name="originalTag">The tag to be replaced.</param>
    /// <returns>The maximum length available for renaming</returns>
    [Authorize(UnitySelector.SettingManagement.Tags.Update)]
    public async Task<int> GetMaxRenameLengthAsync(string originalTag)
    {
        Check.NotNullOrWhiteSpace(originalTag, nameof(originalTag));
        return await _applicationTagsRepository.GetMaxRenameLengthAsync(originalTag);
    }

    /// <summary>
    /// Renames a tag across all application tags, replacing the original tag with the replacement tag.
    /// Only whole-word tags are replaced; substring matches are ignored.
    /// Throws a BusinessException if the original and replacement tags are the same.
    /// </summary>
    /// <param name="originalTag">The tag to be replaced.</param>
    /// <param name="replacementTag">The new tag to use as a replacement.</param>
    /// <returns>A list of IDs for the ApplicationTags entities that were updated.</returns>
    /// <exception cref="BusinessException">Thrown if the original and replacement tags are the same.</exception>
    [Authorize(UnitySelector.SettingManagement.Tags.Update)]
    public async Task<List<Guid>> RenameTagAsync(string originalTag, string replacementTag)
    {
        Check.NotNullOrWhiteSpace(originalTag, nameof(originalTag));
        Check.NotNullOrWhiteSpace(replacementTag, nameof(replacementTag));

        // Remove commas and trim whitespace from tags
        originalTag = originalTag.Replace(",", string.Empty).Trim();
        replacementTag = replacementTag.Replace(",", string.Empty).Trim();

        if (string.Equals(originalTag, replacementTag, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new BusinessException("Cannot update a tag to itself.");
        }

        var applicationTags = await _applicationTagsRepository
            .GetListAsync(e => e.Tag.Name.Contains(originalTag));

        if (applicationTags.Count == 0)
            return [];

        int maxRemainingLength = await GetMaxRenameLengthAsync(originalTag);
        if (replacementTag.Length > maxRemainingLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(replacementTag),
                $"String length exceeds maximum allowed length of {maxRemainingLength}. Actual length: {replacementTag.Length}"
            );
        }

        var updatedTags = new List<ApplicationTags>(applicationTags.Count);

        foreach (var item in applicationTags)
        {
            // Split and trim tags, use case-insensitive HashSet for matching
            var tagSet = new HashSet<string>(
                item.Tag.Name.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    StringComparer.InvariantCultureIgnoreCase);

            // Only replace if the original tag exists (case-insensitive)
            if (tagSet.Remove(originalTag))
            {
                tagSet.Add(replacementTag); // No effect if replacement already exists
                item.Tag.Name = string.Join(',', tagSet.OrderBy(t => t, StringComparer.InvariantCultureIgnoreCase));
                updatedTags.Add(item);
            }
        }

        if (updatedTags.Count > 0)
        {
            await _applicationTagsRepository.UpdateManyAsync(updatedTags, autoSave: true);
        }

        return [.. updatedTags.Select(x => x.Id)];
    }

    /// <summary>
    /// Deletes a tag from all applications and payment requests.
    /// </summary>
    /// <param name="deleteTag">String of tag to be deleted.</param>
    [Authorize(UnitySelector.SettingManagement.Tags.Update)]
    public virtual async Task RenameTagGlobalAsync(string originalTag, string replacementTag)
    {
        Check.NotNullOrWhiteSpace(originalTag, nameof(originalTag));
        Check.NotNullOrWhiteSpace(replacementTag, nameof(replacementTag));

        // NOTE: Unable to get the MIN of the MaxRenameLength for both Application and Payments. Must get on front-end by 2 API calls.
        // May result in one EntityType tag renaming with the other failing in rare cases.

        await RenameTagAsync(originalTag, replacementTag);
        await _localEventBus.PublishAsync(
            new RenameTagEto
            {
                originalTagName = originalTag,
                replacementTagName = replacementTag
            }
        );
    }

    /// <summary>
    /// Deletes a tag from all application tags. Only whole-word tags are removed; substring matches are ignored.
    /// </summary>
    /// <param name="deleteTag">String of tag to be deleted.</param>
    [Authorize(UnitySelector.SettingManagement.Tags.Delete)]
    public async Task DeleteTagWithTagIdAsync(Guid tagId)
    {

        var existingApplicationTags = await _applicationTagsRepository.GetListAsync(e => e.Tag.Id == tagId);
        var idsToDelete = existingApplicationTags.Select(x => x.Id).ToList();
        await _applicationTagsRepository.DeleteManyAsync(idsToDelete, autoSave: true);
        

      
    }

    [Authorize(UnitySelector.SettingManagement.Tags.Delete)]
    public async Task DeleteTagAsync(Guid id)
    {

        await _applicationTagsRepository.DeleteAsync(id);
    }

}