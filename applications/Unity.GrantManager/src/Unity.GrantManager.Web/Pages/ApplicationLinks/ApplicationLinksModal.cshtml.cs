using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicationLinks
{
    class NewLinkItem
    {
        public string? ApplicationId { get; set; }
        public string? CommonText { get; set; }
        public string? UncommonText { get; set; }
    }
    public class ApplicationLinksModalModel : AbpPageModel
    {
        [BindProperty]
        [DisplayName("")]
        public string? SelectedApplications { get; set; } = string.Empty;

        [BindProperty]
        public string? AllApplications { get; set; } = string.Empty;

        [BindProperty]
        public string? SelectedApplicationId { get; set; } = string.Empty;

        [BindProperty]
        public string? ActionType { get; set; } = string.Empty;

        private readonly IApplicationLinksService _applicationLinksService;

        private readonly IGrantApplicationAppService _grantApplicationAppService;


        [BindProperty]
        public string? CommonTags { get; set; } = string.Empty;

        [BindProperty]
        public string? UncommonTags { get; set; } = string.Empty;

        [BindProperty]
        public string? Tags { get; set; } = string.Empty;


        public ApplicationLinksModalModel(IApplicationLinksService applicationLinksService, IGrantApplicationAppService grantApplicationAppService)
        {
            _applicationLinksService = applicationLinksService ?? throw new ArgumentNullException(nameof(applicationLinksService));
            _grantApplicationAppService = grantApplicationAppService ?? throw new ArgumentNullException(nameof(grantApplicationAppService));
        }

        public async Task OnGetAsync(Guid applicationId)
        {
            try
            {
                var allApplications = await _grantApplicationAppService.GetAllApplicationsAsync();

                var linkedApplications = await _applicationLinksService.GetListByApplicationAsync(applicationId);

                var formattedAllApplications = allApplications.Select(item => item.ReferenceNo + " - " + item.ProjectName).ToList();

                var formattedLinkedApplications = linkedApplications.Select(item => item.ReferenceNumber + " - " + item.ProjectName).ToList();

                AllApplications = string.Join(",", formattedAllApplications);
                SelectedApplications = string.Join(",", formattedLinkedApplications);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "Error loading tag select list");
            }
        }

        // public async Task<IActionResult> OnPostAsync()
        // {
        //     const string uncommonTags = "Uncommon Tags"; // Move to constants?
        //     if (SelectedApplicationIds == null) return NoContent();

        //     try
        //     {
        //         var applicationIds = JsonConvert.DeserializeObject<List<Guid>>(SelectedApplicationIds);
        //         if (SelectedTags != null)
        //         {
        //             string[]? stringArray = JsonConvert.DeserializeObject<string[]>(SelectedTags);

        //             if (null != applicationIds)
        //             {
        //                 var selectedApplicationIds = applicationIds.ToArray();

        //                 if (Tags != null)
        //                 {
        //                     var tags = JsonConvert.DeserializeObject<NewTagItem[]>(Tags)?.ToList();

        //                     await ProcessTagsAsync(uncommonTags, stringArray, selectedApplicationIds, tags);
        //                 }
        //             }

        //         }

        //     }
        //     catch (Exception ex)
        //     {
        //         Logger.LogError(ex, message: "Error updating application tags");
        //     }

        //     return NoContent();
        // }

        // private async Task ProcessTagsAsync(string uncommonTags, string[]? stringArray, Guid[] selectedApplicationIds, List<NewTagItem>? tags)
        // {
        //     foreach (var item in selectedApplicationIds)
        //     {
        //         var applicationTagString = "";

        //         if (tags != null
        //             && tags.Count > 0
        //             && stringArray != null
        //             && stringArray.Length > 0
        //             && stringArray.Contains(uncommonTags))
        //         {
        //             NewTagItem? applicationTag = tags.Find(tagItem => tagItem.ApplicationId == item.ToString());

        //             if (applicationTag != null)
        //             {
        //                 applicationTagString += applicationTag.UncommonText;
        //             }
        //         }
        //         if (stringArray != null && stringArray.Length > 0)
        //         {
        //             var applicationCommonTagArray = stringArray.Where(item => item != uncommonTags).ToArray();
        //             if (applicationCommonTagArray.Length > 0)
        //             {
        //                 applicationTagString += (applicationTagString == "" ? string.Join(",", applicationCommonTagArray) : (',' + string.Join(",", applicationCommonTagArray)));

        //             }
        //         }

        //         await _applicationTagsService.CreateorUpdateTagsAsync(item, new ApplicationTagsDto { ApplicationId = item, Text = RemoveDuplicates(applicationTagString) });
        //     }
        // }

        private string RemoveDuplicates(string applicationTagString)
        {
            var tagArray = applicationTagString.Split(",");
            var noDuplicates = tagArray.Distinct().ToArray();
            return string.Join(",", noDuplicates);
        }
    }
}
