using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Identity;

namespace Unity.GrantManager.Web.Pages.ApplicationTags
{
    public class ApplicationTagsModalModel : AbpPageModel
    {
        [BindProperty]
        public String SelectedTags  { get; set; } = string.Empty;

        [BindProperty]
        public List<String> AllTags { get; set; } = new();

        [BindProperty]
        public string SelectedApplicationIds { get; set; } = string.Empty;

        [BindProperty]
        public string ActionType { get; set; } = string.Empty;

        private readonly IApplicationTagsService _applicatioTagsService;


        [BindProperty]
        public string CommonTags { get; set; } = string.Empty;

        [BindProperty]
        public string UncommonTags { get; set; } = string.Empty;


        public ApplicationTagsModalModel(IApplicationTagsService applicatioTagsService)
        {
            _applicatioTagsService = applicatioTagsService ?? throw new ArgumentNullException(nameof(applicatioTagsService));
       
        }

     

        public async Task OnGetAsync(string applicationIds, string actionType)
        {
            SelectedApplicationIds = applicationIds;
            ActionType = actionType;
            AllTags ??= new List<String>();

            try
            {
                 var tags = await _applicatioTagsService.GetListAsync();
                 var groupedTexts = tags
                .Select(item => new { item.ApplicationId , GroupedText = item.Text.Split(',') })
                .SelectMany(item => item.GroupedText.Select(text => new { item.ApplicationId, text }))
                .GroupBy(item => item.text);

                // Separate common and uncommon texts
                var commonTexts = groupedTexts.Where(group => group.Count() > 1)
                    .ToDictionary(group => group.Key, group => group.Select(item => item.ApplicationId).ToArray());

                var uncommonTexts = groupedTexts.Where(group => group.Count() == 1)
                    .ToDictionary(group => group.Key, group => group.Single().ApplicationId);

                // Display the results
               
                UncommonTags = uncommonTexts.ToString();
                //Console.WriteLine("Common Texts:");
          
                List<string> commonTagList   = new List<string>();
                foreach (var kvp in commonTexts)
                {

                    commonTagList.Add(kvp.Key);
                    Console.WriteLine($"Text: {kvp.Key}, ApplicationIds: {string.Join(", ", kvp.Value)}");
                }
                CommonTags = string.Join(",", commonTagList.ToArray());

                Console.WriteLine("\nUncommon Texts:");
                List<string> unCommonTagList = new List<string>();
                foreach (var kvp in uncommonTexts)
                {
                    unCommonTagList.Add(kvp.Key);
                    Console.WriteLine($"Text: {kvp.Key}, ApplicationId: {kvp.Value}");
                }
                UncommonTags = string.Join(",", unCommonTagList.ToArray());


            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "Error loading users select list");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var applicationIds = JsonConvert.DeserializeObject<List<Guid>>(SelectedApplicationIds);
                string[] stringArray = JsonConvert.DeserializeObject<string[]>(SelectedTags);
                //List<string> stringList = stringArray.ToList();
                if (null != applicationIds)
                  
                {
                    var tags = "";
                    Console.WriteLine(SelectedTags);
                    if (stringArray.Contains("CommonTags")) {

                        tags += CommonTags;
                        stringArray = stringArray.Where(item => item != "CommonTags").ToArray();
                    }
                   if(stringArray.Length > 0)
                    {
                        tags += string.Join(", ", stringArray);
                    }
                    var selectedApplicationIds = applicationIds.ToArray();

                    foreach (var item in selectedApplicationIds)
                    {
                        await _applicatioTagsService.CreateorUpdateTagsAsync(item,new ApplicationTagsDto { ApplicationId = item , Text = tags });
                    }
                   

                    //var selectedUser = await _identityUserLookupAppService.FindByIdAsync(AssigneeId);
                    //var userName = $"{selectedUser.Name} {selectedUser.Surname}";

                    //if (ActionType == AssigneeConsts.ACTION_TYPE_ADD)
                    //{
                    //    await _applicationService.InsertAssigneeAsync(applicationIds.ToArray(), AssigneeId);
                    //}
                    //else if (ActionType == AssigneeConsts.ACTION_TYPE_REMOVE)
                    //{
                    //    await _applicationService.DeleteAssigneeAsync(applicationIds.ToArray(), AssigneeId);
                    //}
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "Error updating application status");
            }

            return NoContent();
        }
    }
}
