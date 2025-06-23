using System;
using System.Collections.Generic;
using Unity.Flex.Worksheets;

namespace Unity.GrantManager.GrantApplications;

public class UpdateApplicantInfoDto : CustomDataFieldDto
{
    public Guid ApplicationId { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid ApplicationFormId { get; set; }

    public string? ElectoralDistrict { get; set; }

    public ApplicantSummaryDto? ApplicantSummary { get; set; }
    public UpdateApplicantAddressDto? PhysicalAddress { get; set; }
    public UpdateApplicantAddressDto? MailingAddress { get; set; }
    public SigningAuthorityDto? SigningAuthority { get; set; }
    public ContactInfoDto? ContactInfo { get; set; }
}
