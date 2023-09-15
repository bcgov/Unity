using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Users;
using Microsoft.Extensions.Configuration;


namespace Unity.GrantManager.Web.Pages.GrantApplications
{
    [Authorize]
    public class DetailsModel : AbpPageModel
    {        
        private readonly GrantApplicationAppService _grantApplicationAppService;

        [BindProperty(SupportsGet = true)]
        public string SubmissionId { get; set; }
        public string SelectedAction { get; set; }
        public IFormFile? Attachment { get; set; } = default;
        public List<SelectListItem> ActionList { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "true", Text = "Yes"},
            new SelectListItem { Value = "false", Text = "No"}
        };

        [BindProperty(SupportsGet = true)]
        public Guid ApplicationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ApplicationFormSubmissionId { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public Guid? CurrentUserId { get; set; }

        public string Extensions { get; set; }
        public string MaxFileSize { get; set; }

        private readonly IConfiguration _configuration;


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public DetailsModel(GrantApplicationAppService grantApplicationAppService, IFileAppService fileAppService, ICurrentUser currentUser, IConfiguration configuration)
        {            
            _grantApplicationAppService = grantApplicationAppService;
            CurrentUserId = currentUser.Id;
            _configuration = configuration;
            Extensions =  _configuration["S3:FileTypes"] ?? "";
            MaxFileSize = _configuration["S3:MaxFileSize"] ?? "";
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        
        public async Task OnGetAsync()
        {
            var applicationFormSubmission = await _grantApplicationAppService.GetFormSubmissionByApplicationId(ApplicationId);
            
            if (applicationFormSubmission != null)
            {
                ApplicationFormSubmissionId = applicationFormSubmission.ChefsSubmissionGuid;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await Task.CompletedTask;
            return Page();
        }        
    }    
}
