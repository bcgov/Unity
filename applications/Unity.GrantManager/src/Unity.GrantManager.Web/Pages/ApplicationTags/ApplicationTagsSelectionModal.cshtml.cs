using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NUglify.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        private readonly IApplicationTagsService _applicatioTagsService;


        [BindProperty]
        public string? CommonTags { get; set; } = string.Empty;

        [BindProperty]
        public string? UncommonTags { get; set; } = string.Empty;

        [BindProperty]
        public string? Tags { get; set; } = string.Empty;


        public ApplicationTagsModalModel(IApplicationTagsService applicatioTagsService)
        {
            _applicatioTagsService = applicatioTagsService ?? throw new ArgumentNullException(nameof(applicatioTagsService));

        }



        public async Task OnGetAsync(string applicationIds, string actionType)
        {

            SelectedApplicationIds = applicationIds;
            ActionType = actionType;


            try
            {
                var allTags = await _applicatioTagsService.GetListAsync();
                var applications = JsonConvert.DeserializeObject<List<Guid>>(SelectedApplicationIds);
                var tags = await _applicatioTagsService.GetListWithApplicationIdsAsync(applications);

                var newArray = tags.Select(item =>
                {

                    var textValues = item.Text.Split(',');
                    var commonText = tags.Count == 1
                        ? textValues
                        : tags
                            .Where(x => x.ApplicationId != item.ApplicationId)
                            .SelectMany(x => x.Text.Split(','))
                            .Intersect(textValues)
                            .Distinct();
                    var uncommonText = textValues.Except(commonText);

                    return new NewTagItem
                    {
                        ApplicationId = item.ApplicationId.ToString(),
                        CommonText = string.Join(",", commonText),
                        UncommonText = string.Join(",", uncommonText)
                    };
                }).ToArray();

                var allUniqueCommonTexts = newArray
                    .SelectMany(item => item.CommonText.Split(','))
                    .Distinct()
                    .OrderBy(text => text);
                var allUniqueUncommonTexts = newArray
                    .SelectMany(item => item.UncommonText.Split(','))
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

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var applicationIds = JsonConvert.DeserializeObject<List<Guid>>(SelectedApplicationIds);
                string[] stringArray = JsonConvert.DeserializeObject<string[]>(SelectedTags);
                if (null != applicationIds)

                {

                    var selectedApplicationIds = applicationIds.ToArray();
                    NewTagItem[] tags = JsonConvert.DeserializeObject<NewTagItem[]>(Tags);
                    foreach (var item in selectedApplicationIds)
                    {
                        var applicationTagString = "";

                        Console.WriteLine(SelectedTags);
                        if (stringArray.Contains("Uncommon Tags"))
                        {
                            var applicationTag = tags.FirstOrDefault(tagItem => tagItem.ApplicationId == item.ToString());

                            applicationTagString += applicationTag.UncommonText;

                        }
                        if (stringArray.Length > 0)
                        {
                            var applicationCommonTagArray = stringArray.Where(item => item != "Uncommon Tags").ToArray();
                            applicationTagString += (applicationTagString == "" ? string.Join(",", applicationCommonTagArray) : (',' + string.Join(", ", applicationCommonTagArray)));
                        }


                        await _applicatioTagsService.CreateorUpdateTagsAsync(item, new ApplicationTagsDto { ApplicationId = item, Text = applicationTagString });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "Error updating application tags");
            }

            return NoContent();
        }
    }
}
