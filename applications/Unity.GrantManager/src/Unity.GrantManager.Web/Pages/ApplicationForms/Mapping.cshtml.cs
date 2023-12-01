
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicationForms
{
    [Authorize]
    public class MappingModel : AbpPageModel
    {            

        [BindProperty(SupportsGet = true)]
        public Guid ApplicationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ChefsFormVersionGuid { get; set; }

        private readonly IApplicationFormAppService _applicationFormAppService;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;
        
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

        public MappingModel(IApplicationFormAppService applicationFormAppService,
                            IApplicationFormVersionAppService applicationFormVersionAppService)
        {
            _applicationFormAppService = applicationFormAppService;
            _applicationFormVersionAppService = applicationFormVersionAppService;
        }

        public async Task OnGetAsync()
        {
            ApplicationFormDto = await _applicationFormAppService.GetAsync(ApplicationId);
            ApplicationFormVersionDtoList = (List<ApplicationFormVersionDto>?) await _applicationFormVersionAppService.GetListAsync(ApplicationFormDto.Id);

            foreach (ApplicationFormVersionDto applicationFormVersionDto in ApplicationFormVersionDtoList) {
                if (ChefsFormVersionGuid != null && applicationFormVersionDto.ChefsFormVersionGuid != null && Guid.Parse(applicationFormVersionDto.ChefsFormVersionGuid) == ChefsFormVersionGuid)
                {
                    ApplicationFormVersionDto = applicationFormVersionDto;
                    break;
                }
                else if (ChefsFormVersionGuid == null && applicationFormVersionDto.Published) // If published set as default edit
                {
                    ApplicationFormVersionDto = applicationFormVersionDto;
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
            else if(ApplicationFormVersionDto == null)
            {
                ApplicationFormVersionDto = ApplicationFormVersionDtoList.First();
            }

            ApplicationFormVersionDtoString = JsonSerializer.Serialize(ApplicationFormVersionDto);

            IntakeMapping intakeMapping = new IntakeMapping();
            List<string> properties = new List<string>();
            foreach (var property in intakeMapping.GetType().GetProperties())
            {
                properties.Add("{ \"Name\": \"" + property.Name + "\", \"Type\": \"" + property.PropertyType.Name + "\"}");
            }

            IntakeProperties = JsonSerializer.Serialize(properties);
        }
    }
}
