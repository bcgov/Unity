using Microsoft.AspNetCore.Mvc;
using Unity.ApplicantPortal.Web.Services;
using Unity.ApplicantPortal.Web.ViewModels;

namespace Unity.ApplicantPortal.Web.Views.Shared.Components.ApplicantSubmissions;

public class ApplicantSubmissionsViewComponent(GrantManagerClient grantManagerClient) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var submisisons = await grantManagerClient.GetApplicantSubmissions();

        var submissionList = submisisons?.Select(item => new ApplicantSubmissionsViewModel()
        {
            SubmissionId = item.SubmissionId,
            ReferenceNo = item.ReferenceNo ?? "-",
            Status = item.Status,
            SubmissionDate = item.SubmissionDate
        }).ToList();

        return View(submissionList);
    }
}
