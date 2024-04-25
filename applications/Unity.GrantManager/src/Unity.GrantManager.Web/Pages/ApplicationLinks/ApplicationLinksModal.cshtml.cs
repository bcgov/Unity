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

namespace Unity.GrantManager.Web.Pages.ApplicationLinks
{
    public class ApplicationLinksModalModel : AbpPageModel
    {
        [BindProperty]
        [DisplayName("")]
        public string? SelectedApplications { get; set; } = string.Empty;

        [BindProperty]
        public string? AllApplications { get; set; } = string.Empty;

        [BindProperty]
        public string? GrantApplicationsList { get; set; } = string.Empty;

        [BindProperty]
        public string? LinkedApplicationsList { get; set; } = string.Empty;

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

        [BindProperty]
        public Guid? CurrentApplicationId { get; set; }


        public ApplicationLinksModalModel(IApplicationLinksService applicationLinksService, IGrantApplicationAppService grantApplicationAppService)
        {
            _applicationLinksService = applicationLinksService ?? throw new ArgumentNullException(nameof(applicationLinksService));
            _grantApplicationAppService = grantApplicationAppService ?? throw new ArgumentNullException(nameof(grantApplicationAppService));
        }

        public async Task OnGetAsync(Guid applicationId)
        {
            try
            {
                CurrentApplicationId = applicationId;
                var grantApplications = await _grantApplicationAppService.GetAllApplicationsAsync();
                var linkedApplications = await _applicationLinksService.GetListByApplicationAsync(applicationId);

                // remove current application id from ths suggestion list
                grantApplications.Remove(grantApplications.Single(item => item.Id == applicationId));

                var formattedAllApplications = grantApplications.Select(item => item.ReferenceNo + " - " + item.ProjectName).ToList();
                var formattedLinkedApplications = linkedApplications.Select(item => item.ReferenceNumber + " - " + item.ProjectName).ToList();

                AllApplications = string.Join(",", formattedAllApplications);
                SelectedApplications = string.Join(",", formattedLinkedApplications);
                GrantApplicationsList = JsonConvert.SerializeObject(grantApplications);
                LinkedApplicationsList = JsonConvert.SerializeObject(linkedApplications);
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
                if (SelectedApplications != null) {
                    string[]? selectedApplicationsArray = JsonConvert.DeserializeObject<string[]>(SelectedApplications);
                    List<GrantApplicationLiteDto>? grantApplications = JsonConvert.DeserializeObject<List<GrantApplicationLiteDto>>(GrantApplicationsList!);
                    List<ApplicationLinksInfoDto>? linkedApplications = JsonConvert.DeserializeObject<List<ApplicationLinksInfoDto>>(LinkedApplicationsList!);

                    foreach (var item in selectedApplicationsArray!)
                    {
                        var itemArr = item.Split('-');
                        var referenceNo = itemArr[0].Trim();
                        ApplicationLinksInfoDto applicationLinksInfoDto = linkedApplications!.Find(application => application.ReferenceNumber == referenceNo)!;
                        
                        // Add new link only if its not already existing
                        if (applicationLinksInfoDto == null) {
                            Guid linkedApplicationId = grantApplications!.Find(application => application.ReferenceNo == referenceNo)!.Id;

                            await _applicationLinksService.CreateAsync(new ApplicationLinksDto{
                                ApplicationId = CurrentApplicationId ?? Guid.Empty,
                                LinkedApplicationId = linkedApplicationId
                            });
                        }
                    }

                    // For removing the deleted links
                    foreach (ApplicationLinksInfoDto linked in linkedApplications!)
                    {
                        var selectedIndex = selectedApplicationsArray!.FindIndex(selected => selected.Split('-')[0].Trim() == linked.ReferenceNumber);
                        if(selectedIndex < 0) {
                            await _applicationLinksService.DeleteAsync(linked.Id);
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
    }
}
