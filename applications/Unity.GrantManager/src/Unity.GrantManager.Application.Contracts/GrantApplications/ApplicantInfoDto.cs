using System;
using System.Collections.Generic;
using Unity.Flex.Worksheets;

namespace Unity.GrantManager.GrantApplications;

public class ApplicantInfoDto : CustomDataFieldDto
{
    public Guid ApplicationId { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid ApplicationFormId { get; set; }

    public string ApplicationReferenceNo { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public GrantApplicationState ApplicationStatusCode { get; set; }
    
    // Sourced from the Application.ApplicantElectoralDistrict
    public string? ElectoralDistrict { get; set; }

    public ApplicantSummaryDto? ApplicantSummary { get; set; }
    public List<ApplicantAddressDto>? ApplicantAddresses { get; set; }
    public SigningAuthorityDto? SigningAuthority { get; set; }
    public ContactInfoDto? ContactInfo { get; set; }
}
