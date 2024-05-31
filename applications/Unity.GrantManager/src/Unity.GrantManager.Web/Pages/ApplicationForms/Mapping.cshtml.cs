
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Scoresheets;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicationForms
{
    [Authorize]
    public class MappingModel : AbpPageModel
    {
        public List<SelectListItem> ScoresheetOptionsList { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ApplicationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ChefsFormVersionGuid { get; set; }

        private readonly IApplicationFormAppService _applicationFormAppService;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;
        private readonly IScoresheetAppService _scoresheetAppService;

        [BindProperty]
        public ApplicationFormDto? ApplicationFormDto { get; set; }

        [BindProperty]
        public ApplicationFormVersionDto? ApplicationFormVersionDto { get; set; }

        [BindProperty]
        public List<ApplicationFormVersionDto>? ApplicationFormVersionDtoList { get; set; }

        [BindProperty]
        public string? ApplicationFormVersionDtoString { get; set; }

        [BindProperty]
        public string? IntakeProperties { get; set; }

        [BindProperty]
        [Display(Name = "")]
        public Guid? ScoresheetId { get; set; }

        public MappingModel(IApplicationFormAppService applicationFormAppService,
                            IApplicationFormVersionAppService applicationFormVersionAppService,
                            IScoresheetAppService scoresheetAppService)
        {
            _applicationFormAppService = applicationFormAppService;
            _applicationFormVersionAppService = applicationFormVersionAppService;
            _scoresheetAppService = scoresheetAppService;
        }

        public async Task OnGetAsync()
        {
            ApplicationFormDto = await _applicationFormAppService.GetAsync(ApplicationId);
            ScoresheetId = ApplicationFormDto.ScoresheetId;
            ApplicationFormVersionDtoList = (List<ApplicationFormVersionDto>?)await _applicationFormAppService.GetVersionsAsync(ApplicationFormDto.Id);

            if (ApplicationFormVersionDtoList != null)
            {
                foreach (ApplicationFormVersionDto applicationFormVersionDto in ApplicationFormVersionDtoList)
                {
                    if ((applicationFormVersionDto.ChefsFormVersionGuid != null && Guid.Parse(applicationFormVersionDto.ChefsFormVersionGuid) == ChefsFormVersionGuid)
                    || (ChefsFormVersionGuid.ToString() == "00000000-0000-0000-0000-000000000000" && applicationFormVersionDto.Published))
                    {
                        ApplicationFormVersionDto = applicationFormVersionDto;
                        if (ChefsFormVersionGuid.ToString() == "00000000-0000-0000-0000-000000000000" && applicationFormVersionDto.ChefsFormVersionGuid != null)
                        {
                            ChefsFormVersionGuid = Guid.Parse(applicationFormVersionDto.ChefsFormVersionGuid);
                        }
                        break;
                    }
                }

                if (ApplicationFormVersionDtoList.Count == 0 && ApplicationFormVersionDto == null)
                {
                    CreateUpdateApplicationFormVersionDto appFormVersion = new CreateUpdateApplicationFormVersionDto();
                    appFormVersion.ApplicationFormId = ApplicationFormDto.Id;
                    appFormVersion.ChefsApplicationFormGuid = ApplicationFormDto.ChefsApplicationFormGuid;
                    ApplicationFormVersionDto = await _applicationFormVersionAppService.CreateAsync(appFormVersion);
                }
                else if (ApplicationFormVersionDto == null)
                {
                    ApplicationFormVersionDto = ApplicationFormVersionDtoList.First();
                }

                ApplicationFormVersionDtoString = JsonSerializer.Serialize(ApplicationFormVersionDto);
            }



            IntakeMapping intakeMapping = new IntakeMapping();
            List<string> properties = new List<string>();
            foreach (var property in intakeMapping.GetType().GetProperties())
            {
                properties.Add("{ \"Name\": \"" + property.Name + "\", \"Type\": \"" + property.PropertyType.Name + "\"}");
            }

            IntakeProperties = JsonSerializer.Serialize(properties);
            var scoresheets = await _scoresheetAppService.GetAllScoresheetsAsync();
            ScoresheetOptionsList = new List<SelectListItem>();
            foreach (var scoresheet in scoresheets)
            {
                ScoresheetOptionsList.Add(new SelectListItem { Text = scoresheet.Name + " V" + scoresheet.Version + ".0", Value =scoresheet.Id.ToString()});
            }
            ScoresheetOptionsList = ScoresheetOptionsList.OrderBy(item => item.Text).ToList();
        }
    }
}
