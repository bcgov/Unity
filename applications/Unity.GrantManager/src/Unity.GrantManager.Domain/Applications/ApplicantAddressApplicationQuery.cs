using System;
using System.Linq;

namespace Unity.GrantManager.Applications;

public static class ApplicantAddressApplicationQuery
{
    // Shared by the form and save endpoint so both select the same submission.
    public static IOrderedQueryable<Application> ForApplicantAddressCreation(
        this IQueryable<Application> applications, Guid applicantId)
    {
        return applications
            .Where(application => application.ApplicantId == applicantId
                && !application.IsDeleted
                && application.Id != Guid.Empty)
            .OrderByDescending(application => application.SubmissionDate)
            .ThenByDescending(application => application.CreationTime)
            .ThenByDescending(application => application.Id);
    }
}
