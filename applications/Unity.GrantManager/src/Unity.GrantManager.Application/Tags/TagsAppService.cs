using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.Modules.Shared;
using Unity.Payments.Events;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;

namespace Unity.GrantManager.GlobalTag;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(TagsAppService), typeof(ITagsService))]
public class TagsAppService : ApplicationService, ITagsService
{
    private readonly ITagsRepository _tagsRepository;
    private readonly IApplicationTagsService _applicationTagsService;
    private readonly ILocalEventBus _localEventBus;

    public TagsAppService(ITagsRepository repository, ILocalEventBus localEventBus, IApplicationTagsService applicationTagsService)
    {
        _tagsRepository = repository;
        _localEventBus = localEventBus;
        _applicationTagsService = applicationTagsService;
    }

    public async Task<IList<TagDto>> GetListAsync()
    {
        var tags = await _tagsRepository.GetListAsync();
        return ObjectMapper.Map<List<Tag>, List<TagDto>>(tags.OrderBy(t => t.Id).ToList());
    }

    [Authorize(UnitySelector.SettingManagement.Tags.Create)]
    public async Task<TagDto> CreateTagsAsync(TagDto input)
    {
        var normalizedName = input.Name.ToLower();
        var tag = await _tagsRepository
            .FirstOrDefaultAsync(e => e.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));

        if (tag != null)
        {
            throw new BusinessException(

               "400", "Another tag with the same name already exists."
            );
        }
        var newTag = await _tagsRepository.InsertAsync(new Tag
        {
            Name = input.Name
        }, autoSave: true);

        return ObjectMapper.Map<Tag, TagDto>(newTag);
    }

    public async Task<TagDto> CreateorUpdateTagsAsync(Guid id, TagDto input)
    {
        var normalizedName = input.Name.ToLower();
        var tag = await _tagsRepository
            .FirstOrDefaultAsync(e => e.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));
        if (tag == null)
        {
            var newTag = await _tagsRepository.InsertAsync(new Tag
            {
                Name = input.Name
            }, autoSave: true);

            return ObjectMapper.Map<Tag, TagDto>(newTag);
        }
        else
        {
            tag.Name = input.Name;
            await _tagsRepository.UpdateAsync(tag, autoSave: true);
            return ObjectMapper.Map<Tag, TagDto>(tag);
        }
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
    public async Task<List<Guid>> RenameTagAsync(Guid id, string originalTag, string replacementTag)
    {
        Check.NotNullOrWhiteSpace(originalTag, nameof(originalTag));
        Check.NotNullOrWhiteSpace(replacementTag, nameof(replacementTag));

        var duplicateTag = await _tagsRepository
           .FindAsync(e => e.Name.Equals(replacementTag) && e.Id != id);
        if (duplicateTag != null)
        {
            throw new BusinessException(

                "400", "Another tag with the same name already exists."
            );
        }

        var tag = await _tagsRepository
            .FindAsync(e => e.Id.Equals(id));

        if (tag == null)
            return [];

        tag.Name = replacementTag;

        await _tagsRepository.UpdateAsync(tag, autoSave: true);

        return [tag.Id];
    }

    /// <summary>
    /// Deletes a tag from all applications and payment requests.
    /// </summary>
    /// <param name="deleteTag">String of tag to be deleted.</param>
    [Authorize(UnitySelector.SettingManagement.Tags.Update)]
    public virtual async Task RenameTagGlobalAsync(Guid id, string originalTag, string replacementTag)
    {
        Check.NotNullOrWhiteSpace(originalTag, nameof(originalTag));
        Check.NotNullOrWhiteSpace(replacementTag, nameof(replacementTag));

        // NOTE: Unable to get the MIN of the MaxRenameLength for both Application and Payments. Must get on front-end by 2 API calls.
        // May result in one EntityType tag renaming with the other failing in rare cases.

        await RenameTagAsync(id, originalTag, replacementTag);
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
    public async Task DeleteTagAsync(Guid id)
    {
        await _tagsRepository.DeleteAsync(id);
    }

    /// <summary>
    /// Deletes a tag from all applications and payment requests.
    /// </summary>
    /// <param name="deleteTag">String of tag to be deleted.</param>
    [Authorize(UnitySelector.SettingManagement.Tags.Delete)]
    public virtual async Task DeleteTagGlobalAsync(Guid id)
    {

        await _applicationTagsService.DeleteTagWithTagIdAsync(id);
        await _localEventBus.PublishAsync(new DeleteTagEto { TagId = id });

    }

    public async Task<PagedResultDto<TagUsageSummaryDto>> GetTagSummaryAsync()
    {
        var summary = await _tagsRepository.GetTagUsageSummaryAsync();

        // Correct the mapping by swapping the source and destination types
        var tagSummary = ObjectMapper.Map<List<TagUsageSummary>, List<TagUsageSummaryDto>>(summary);

        return new PagedResultDto<TagUsageSummaryDto>(
            tagSummary.Count,
            tagSummary // Use the correctly mapped tagSummary here
        );
    }
}