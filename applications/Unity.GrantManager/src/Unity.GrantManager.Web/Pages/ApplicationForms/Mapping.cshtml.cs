
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NUglify.Helpers;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Intakes;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicationForms
{
    [Authorize]
    public class MappingModel : AbpPageModel
    {            

        [BindProperty(SupportsGet = true)]
        public Guid ApplicationId { get; set; }
        private readonly IApplicationFormAppService _applicationFormAppService;
        
        [BindProperty]
        public ApplicationFormDto? ApplicationFormDto { get; set; }

        [BindProperty]
        public string? ApplicationFormDtoString { get; set; }


        [BindProperty]
        public string? IntakeProperties { get; set; }



        public MappingModel(IApplicationFormAppService applicationFormAppService)
        {
            _applicationFormAppService = applicationFormAppService;
        }

        public async Task OnGetAsync()
        {
            ApplicationFormDto = await _applicationFormAppService.GetAsync(ApplicationId);
            ApplicationFormDtoString = JsonSerializer.Serialize(ApplicationFormDto);
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
