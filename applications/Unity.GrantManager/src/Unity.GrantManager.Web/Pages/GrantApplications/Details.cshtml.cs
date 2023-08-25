using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.GrantApplications
{
    [Authorize]
    public class DetailsModel : AbpPageModel
    {
        private readonly AssessmentCommentService _assessmentCommentService;

        [BindProperty(SupportsGet = true)]
        public string SubmissionId { get; set; }
        public string SelectedAction { get; set; }
        public IFormFile Attachment { get; set; } = null;
        public List<SelectListItem> ActionList { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Y", Text = "Recommended for Approval"},
            new SelectListItem { Value = "N", Text = "Recommended for Denial"}
        };

        [TextArea]
        [BindProperty(SupportsGet = true)]
        public string? Comment { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? CommentId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ApplicationFormSubmissionId { get; set; }


        [BindProperty]
        public List<AssessmentCommentDto> CommentList { get; set; } = new();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public DetailsModel(AssessmentCommentService assessmentCommentService) => _assessmentCommentService = assessmentCommentService;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public void OnGet()
        {
            var comments = _assessmentCommentService.GetListAsync(Guid.Parse(ApplicationFormSubmissionId));
            if (comments.Result != null && comments.Result.Count > 0)
            {
                CommentList = (List<AssessmentCommentDto>)(comments.Result);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (CommentId != null && CommentId != "" && Comment != null)
                {
                    await _assessmentCommentService.UpdateAssessmentComment(CommentId, Comment);
                }
                else if(Comment != null)
                {
                    await _assessmentCommentService.CreateAssessmentComment(Comment, ApplicationFormSubmissionId);
                }

                var comments = _assessmentCommentService.GetListAsync(Guid.Parse(ApplicationFormSubmissionId));
                if (comments.Result != null && comments.Result.Count > 0)
                {
                    CommentList = (List<AssessmentCommentDto>)(comments.Result);
                    Comment = "";
                    CommentId = "";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return Page();
        }
    }
}
