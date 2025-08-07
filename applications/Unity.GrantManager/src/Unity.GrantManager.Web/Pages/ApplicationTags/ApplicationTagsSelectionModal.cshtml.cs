using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GlobalTag;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicationTags
{
    public class NewTagItem
    {
        public string? ApplicationId { get; set; }
        public List<TagDto> CommonTags { get; set; } = new();
        public List<TagDto> UncommonTags { get; set; } = new();
    }

    public class ApplicationTagsModalModel : AbpPageModel
    {
        [BindProperty]
        [DisplayName("Tags")]
        public string? SelectedTags { get; set; } = string.Empty;

        [BindProperty]
        [DisplayName("All Tags")]
        public List<TagDto> AllTags { get; set; } = new();

        [BindProperty]
        [DisplayName("Selected Applications")]
        public string? SelectedApplicationIds { get; set; } = string.Empty;

        [BindProperty]
        [DisplayName("Action Type")]
        public string? ActionType { get; set; } = string.Empty;

        private readonly IApplicationTagsService _applicationTagsService;

        [BindProperty]
        [DisplayName("Common Tags")]
        public List<TagDto> CommonTags { get; set; } = new();

        [BindProperty]
        [DisplayName("Uncommon Tags")]
        public List<TagDto> UncommonTags { get; set; } = new();

        [BindProperty]
        [DisplayName("Tags")]
        public List<NewTagItem> Tags { get; set; } = new();

        [BindProperty]
        public string? SelectedTagsJson { get; set; }

        [BindProperty]
        public string? TagsJson { get; set; }

        public ApplicationTagsModalModel(IApplicationTagsService applicationTagsService)
        {
            _applicationTagsService = applicationTagsService ?? throw new ArgumentNullException(nameof(applicationTagsService));
        }

        public Task OnGetAsync(string applicationIds, string actionType)
        {
            SelectedApplicationIds = applicationIds;
            ActionType = actionType;

            // Return a completed task to ensure all code paths return a value
            return Task.CompletedTask;
        }
        public async Task<IActionResult> OnPostAsync()
        {
            const string uncommonTags = "Uncommon Tags"; // Move to constants?

            if (SelectedApplicationIds == null) return NoContent();

            try
            {
                var applicationIds = JsonConvert.DeserializeObject<List<Guid>>(SelectedApplicationIds);
                if (SelectedTags != null && applicationIds != null && applicationIds.Count > 0)
                {
                    var selectedTagList = DeserializeJson<List<TagDto>>(SelectedTags) ?? [];
                    var tagItems = string.IsNullOrWhiteSpace(TagsJson) ? null : DeserializeJson<List<NewTagItem>>(TagsJson);
                    await ProcessTagsAsync(uncommonTags, selectedTagList, applicationIds.ToArray(), tagItems);

                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "Error updating application tags");
            }

            return NoContent();
        }

        private async Task ProcessTagsAsync(string uncommonTagsLabel, List<TagDto> selectedTags, Guid[] selectedApplicationIds, List<NewTagItem>? tags)
        {
            for (int i = 0; i < selectedApplicationIds.Length; i++)
            {
                var item = selectedApplicationIds[i];
                var tagList = new List<TagDto>();

                if (selectedTags.Any(t => t.Name == uncommonTagsLabel) && tags != null)
                {
                    var applicationTag = tags.FirstOrDefault(tagItem => tagItem.ApplicationId == item.ToString());
                    if (applicationTag?.UncommonTags != null)
                    {
                        tagList.AddRange(applicationTag.UncommonTags);
                    }
                }

                var commonTagsOnly = selectedTags
                    .Where(tag => tag.Name != uncommonTagsLabel)
                    .ToList();

                tagList.AddRange(commonTagsOnly);

                var distinctTags = tagList
                     .Where(tag => tag != null && tag.Id != Guid.Empty)
                     .GroupBy(tag => tag.Id)
                     .Select(group => group.First())
                     .ToList();

                try
                {
                    await _applicationTagsService.AssignTagsAsync(new AssignApplicationTagsDto
                    {
                        ApplicationId = item,
                        Tags = distinctTags
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing ApplicationId {item}: {ex.Message}");

                }
            }
        }
        private static T? DeserializeJson<T>(string jsonString) where T : class
        {
            return string.IsNullOrEmpty(jsonString) ? null : JsonConvert.DeserializeObject<T>(jsonString);
        }
    }
}
