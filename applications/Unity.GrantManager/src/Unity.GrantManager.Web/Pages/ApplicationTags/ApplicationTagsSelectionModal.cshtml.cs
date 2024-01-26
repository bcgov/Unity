using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicationTags
{
    class NewTagItem
    {
        public string? ApplicationId { get; set; }
        public string? CommonText { get; set; }
        public string? UncommonText { get; set; }
    }
    public class ApplicationTagsModalModel : AbpPageModel
    {
        [BindProperty]
        [DisplayName("")]
        public string? SelectedTags { get; set; } = string.Empty;

        [BindProperty]
        public string? AllTags { get; set; } = string.Empty;

        [BindProperty]
        public string? SelectedApplicationIds { get; set; } = string.Empty;

        [BindProperty]
        public string? ActionType { get; set; } = string.Empty;

        private readonly IApplicationTagsService _applicationTagsService;


        [BindProperty]
        public string? CommonTags { get; set; } = string.Empty;

        [BindProperty]
        public string? UncommonTags { get; set; } = string.Empty;

        [BindProperty]
        public string? Tags { get; set; } = string.Empty;


        public ApplicationTagsModalModel(IApplicationTagsService applicatioTagsService)
        {
            _applicationTagsService = applicatioTagsService ?? throw new ArgumentNullException(nameof(applicatioTagsService));

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
                    var allTags = await _applicationTagsService.GetListAsync();

                    var tags = await _applicationTagsService.GetListWithApplicationIdsAsync(applications);

                    // Add default objects for missing applicationIds
                    var missingApplicationIds = applications.Except(tags.Select(tag => tag.ApplicationId));
                    tags = tags.Concat(missingApplicationIds.Select(appId => new ApplicationTagsDto
                    {
                        ApplicationId = appId,
                        Text = "", // You can set default values here
                        Id = Guid.NewGuid() // Assuming Id is a Guid
                    })).ToList();

                    var newArray = tags.Select(item =>
                    {
                        var textValues = item.Text.Split(',');
                        var commonText = tags
                            .SelectMany(x => x.Text.Split(','))
                            .GroupBy(text => text)
                            .Where(group => group.Count() == tags.Count)
                            .Select(group => group.Key);

                        var uncommonText = textValues.Except(commonText);

                        return new NewTagItem
                        {
                            ApplicationId = item.ApplicationId.ToString(),
                            CommonText = string.Join(",", commonText),
                            UncommonText = string.Join(",", uncommonText)
                        };
                    }).ToArray();

                    var allUniqueCommonTexts = newArray
                        .SelectMany(item => (item.CommonText?.Split(',') ?? Array.Empty<string>()))
                        .Where(text => !string.IsNullOrEmpty(text))
                        .Distinct()
                        .OrderBy(text => text);

                    var allUniqueUncommonTexts = newArray
                        .SelectMany(item => (item.UncommonText?.Split(',') ?? Array.Empty<string>()))
                        .Where(text => !string.IsNullOrEmpty(text))
                        .Distinct()
                        .OrderBy(text => text);



                    var allUniqueTexts = allTags
                                        .SelectMany(obj => obj.Text.ToString().Split(',').Select(t => t.Trim()))
                                        .Distinct();
                    var uniqueCommonTextsString = string.Join(",", allUniqueCommonTexts);
                    var uniqueUncommonTextsString = string.Join(",", allUniqueUncommonTexts);
                    var allUniqueTextsString = string.Join(",", allUniqueTexts);

                    AllTags = allUniqueTextsString;
                    CommonTags = uniqueCommonTextsString;
                    UncommonTags = uniqueUncommonTextsString;
                    Tags = JsonConvert.SerializeObject(newArray);
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
                if (SelectedTags != null)
                {
                    string[]? stringArray = JsonConvert.DeserializeObject<string[]>(SelectedTags);

                    if (null != applicationIds)
                    {
                        var selectedApplicationIds = applicationIds.ToArray();

                        if (Tags != null)
                        {
                            var tags = JsonConvert.DeserializeObject<NewTagItem[]>(Tags)?.ToList();

                            await ProcessTagsAsync(uncommonTags, stringArray, selectedApplicationIds, tags);
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "Error updating application tags");
            }

            return NoContent();
        }

        private async Task ProcessTagsAsync(string uncommonTags, string[]? stringArray, Guid[] selectedApplicationIds, List<NewTagItem>? tags)
        {
            foreach (var item in selectedApplicationIds)
            {
                var applicationTagString = "";

                if (tags != null
                    && tags.Count > 0
                    && stringArray != null
                    && stringArray.Length > 0
                    && stringArray.Contains(uncommonTags))
                {
                    NewTagItem? applicationTag = tags.Find(tagItem => tagItem.ApplicationId == item.ToString());

                    if (applicationTag != null)
                    {
                        applicationTagString += applicationTag.UncommonText;
                    }
                }
                if (stringArray != null && stringArray.Length > 0)
                {
                    var applicationCommonTagArray = stringArray.Where(item => item != uncommonTags).ToArray();
                    if (applicationCommonTagArray.Length > 0)
                    {
                        applicationTagString += (applicationTagString == "" ? string.Join(",", applicationCommonTagArray) : (',' + string.Join(",", applicationCommonTagArray)));

                    }
                }

                await _applicationTagsService.CreateorUpdateTagsAsync(item, new ApplicationTagsDto { ApplicationId = item, Text = RemoveDuplicates(applicationTagString) });
            }
        }

        private string RemoveDuplicates(string applicationTagString)
        {
            var tagArray = applicationTagString.Split(",");
            var noDuplicates = tagArray.Distinct().ToArray();
            return string.Join(",", noDuplicates);
        }
    }
}
