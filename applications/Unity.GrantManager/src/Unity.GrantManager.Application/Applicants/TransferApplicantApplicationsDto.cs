using System;

namespace Unity.GrantManager.Applicants;

public class TransferApplicantApplicationsDto
{
    public Guid PrincipalApplicantId { get; set; }
    public Guid NonPrincipalApplicantId { get; set; }
}
