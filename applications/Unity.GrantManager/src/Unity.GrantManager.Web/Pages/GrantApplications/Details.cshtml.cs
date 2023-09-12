using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Unity.GrantManager.Comments;
using System.Xml.Linq;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Web.Views.Shared.Components.ApplicationAttachments;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Users;

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

        [TextArea]
        [BindProperty(SupportsGet = true)]
        public string? Comment { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public Guid? CommentId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ApplicationId { get; set; }        

        [BindProperty(SupportsGet = true)]
        public string ApplicationFormSubmissionId { get; set; }
        
        [BindProperty]
        public List<CommentDto> CommentList { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public Guid? CurrentUserId { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public DetailsModel(GrantApplicationAppService grantApplicationAppService, IFileAppService fileAppService, ICurrentUser currentUser)
        {            
            _grantApplicationAppService = grantApplicationAppService;
            CurrentUserId = currentUser.Id;
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public async Task OnGetAsync()
        {
            var comments = await _grantApplicationAppService.GetCommentsAsync(ApplicationId);
            var applicationFormSubmission = await _grantApplicationAppService.GetFormSubmissionByApplicationId(ApplicationId);
            
            if (applicationFormSubmission != null)
            {
                ApplicationFormSubmissionId = applicationFormSubmission.ChefsSubmissionGuid;
            }

            if (comments != null && comments.Count > 0)
            {
                CommentList = (List<CommentDto>)(comments);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (CommentId != null && Comment != null)
                {
                    await _grantApplicationAppService.UpdateCommentAsync(ApplicationId, new UpdateCommentDto()
                    {
                        Comment = Comment,
                        CommentId = CommentId.Value
                    });
                }
                else if (Comment != null)
                {
                    await _grantApplicationAppService.CreateCommentAsync(ApplicationId, new CreateCommentDto()
                    {
                        Comment = Comment
                    });
                }

                var comments = _grantApplicationAppService.GetCommentsAsync(ApplicationId);

                if (comments.Result != null && comments.Result.Count > 0)
                {
                    CommentList = (List<CommentDto>)(comments.Result);
                    Comment = "";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating or creating application exception");
            }

            return Page();
        }

        
    }
    

}
