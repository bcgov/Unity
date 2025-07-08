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
        private readonly ITagsService _tagsService;

        [BindProperty]
        [DisplayName("Common Tags")]
        public List<TagDto> CommonTags { get; set; } = new();

        [BindProperty]
        [DisplayName("Uncommon Tags")]
        public List<TagDto> UncommonTags { get; set; } = new();

        [BindProperty]
        [DisplayName("Tags")]
        public List<NewTagItem> Tags { get; set; } = new();//

        [BindProperty]
        public string? SelectedTagsJson { get; set; } // receives raw JSON string

        [BindProperty]
        public string? TagsJson { get; set; }

        public ApplicationTagsModalModel(IApplicationTagsService applicationTagsService, ITagsService tagsService)
        {
            _applicationTagsService = applicationTagsService ?? throw new ArgumentNullException(nameof(applicationTagsService));
            _tagsService = tagsService ?? throw new ArgumentNullException(nameof(tagsService));
        }

        public async Task OnGetAsync(string applicationIds, string actionType)
        {
            SelectedApplicationIds = applicationIds;
            ActionType = actionType;

            var applications = JsonConvert.DeserializeObject<List<Guid>>(SelectedApplicationIds);
            if (applications != null && applications.Count > 0)
            {
                try
                {
                    CommonTags = new List<TagDto>();
                    UncommonTags = new List<TagDto>();
                    var allTags = await _tagsService.GetListAsync();
                    var tags = await _applicationTagsService.GetListWithApplicationIdsAsync(applications);
                    var groupedTags = tags
                        .Where(x => x.Tag != null)
                        .GroupBy(x => x.ApplicationId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(x => x.Tag!).DistinctBy(t => t.Id).ToList()
                        );
                    foreach (var missingId in applications.Except(groupedTags.Keys))
                    {
                        groupedTags[missingId] = new List<TagDto>();
                    }
                    List<TagDto> commonTags = new();

                    if (groupedTags.Values.Any())
                    {
                        commonTags = groupedTags.Values
                            .Aggregate((prev, next) => prev.IntersectBy(next.Select(t => t.Id), t => t.Id).ToList());
                    }

                    Tags = groupedTags.Select(kvp =>
                    {
                        var appId = kvp.Key;
                        var tagList = kvp.Value;

                        var uncommonTags = tagList
                            .Where(tag => !commonTags.Any(ct => ct.Id == tag.Id))
                            .ToList();

                        return new NewTagItem
                        {
                            ApplicationId = appId.ToString(),
                            CommonTags = commonTags.OrderBy(t => t.Name).ToList(),
                            UncommonTags = uncommonTags.OrderBy(t => t.Name).ToList()
                        };
                    }).ToList();

                    if (Tags.Count > 0)
                    {

                        CommonTags = Tags
                            .SelectMany(item => item.CommonTags)
                            .GroupBy(tag => tag.Id)
                            .Select(group => group.First())
                            .OrderBy(tag => tag.Name)
                            .ToList();

                        UncommonTags = Tags
                            .SelectMany(item => item.UncommonTags)
                            .GroupBy(tag => tag.Id)
                            .Select(group => group.First())
                            .OrderBy(tag => tag.Name)
                            .ToList();
                    }
                    AllTags = allTags
                        .DistinctBy(tag => tag.Id)
                        .OrderBy(tag => tag.Name)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, message: "Error loading tag select list");
                }
            }
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
                    var tagItems = DeserializeJson<List<NewTagItem>>(TagsJson);
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
