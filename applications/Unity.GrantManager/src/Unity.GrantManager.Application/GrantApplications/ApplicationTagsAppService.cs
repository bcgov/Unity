using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Permissions;
using Volo.Abp;
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

        // Sanitize input tag text string
        var tagInput = input.Text.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        input.Text = string.Join(',', tagInput.OrderBy(t => t, StringComparer.InvariantCultureIgnoreCase));

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

    [Authorize(UnitySettingManagementPermissions.Tags.Default)]
    public async Task<PagedResultDto<TagSummaryCountDto>> GetApplicationTagCounts()
    {
        var tagSummary = ObjectMapper.Map<List<TagSummaryCount>, List<TagSummaryCountDto>>(
            await _applicationTagsRepository.GetTagCounts());

        return new PagedResultDto<TagSummaryCountDto>(
            tagSummary.Count,
            tagSummary
        );
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
    [Authorize(UnitySettingManagementPermissions.Tags.Update)]
    public async Task<List<Guid>> RenameTagAsync(string originalTag, string replacementTag)
    {
        Check.NotNullOrWhiteSpace(originalTag, nameof(originalTag));
        Check.NotNullOrWhiteSpace(replacementTag, nameof(replacementTag));

        // Remove commas from the originalTag and replacementTag
        originalTag = originalTag.Replace(",", string.Empty);
        replacementTag = replacementTag.Replace(",", string.Empty);

        // Check if the originalTag and replacementTag are the same
        if (originalTag == replacementTag)
        {
            throw new BusinessException("Cannot update a tag to itself.");
        }

        var applicationTags = await _applicationTagsRepository
            .GetListAsync(e => e.Text.Contains(originalTag));

        var updatedTags = new List<ApplicationTags>();

        foreach (var item in applicationTags)
        {
            var tagList = item.Text.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            // Only replace whole word tags - skip substring matches
            if (tagList.Remove(originalTag))
            {
                tagList.Add(replacementTag); // HashSet collision if the replacementTag already exists - ignore
                item.Text = string.Join(',', tagList.OrderBy(t => t, StringComparer.InvariantCultureIgnoreCase));
                updatedTags.Add(item);
            }
        }

        if (updatedTags.Count > 0)
        {
            await _applicationTagsRepository.UpdateManyAsync(applicationTags, autoSave: true);
        }

        return [.. updatedTags.Select(x => x.Id)];
    }

    /// <summary>
    /// Deletes a tag from all application tags. Only whole-word tags are removed; substring matches are ignored.
    /// </summary>
    /// <param name="deleteTag">String of tag to be deleted.</param>
    [Authorize(UnitySettingManagementPermissions.Tags.Delete)]
    public async Task DeleteTagAsync(string deleteTag)
    {
        Check.NotNullOrWhiteSpace(deleteTag, nameof(deleteTag));

        // Remove commas from the originalTag and replacementTag
        deleteTag = deleteTag.Replace(",", string.Empty);

        var applicationTags = await _applicationTagsRepository
            .GetListAsync(e => e.Text.Contains(deleteTag));

        var updatedTags = new List<ApplicationTags>();
        var deletedTags = new List<ApplicationTags>();

        foreach (var item in applicationTags)
        {
            var tagList = item.Text.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            // Only replace whole word tags - skip substring matches
            if (tagList.Remove(deleteTag))
            {
                if (tagList.Count > 0)
                {
                    item.Text = string.Join(',', tagList.OrderBy(t => t, StringComparer.InvariantCultureIgnoreCase));
                    updatedTags.Add(item);
                }
                else
                {
                    deletedTags.Add(item);
                }
            }
        }

        if (deletedTags.Count > 0)
        {
            await _applicationTagsRepository.DeleteManyAsync(deletedTags, autoSave: true);
        }

        if (updatedTags.Count > 0)
        {
            await _applicationTagsRepository.UpdateManyAsync(applicationTags, autoSave: true);
        }
    }
}