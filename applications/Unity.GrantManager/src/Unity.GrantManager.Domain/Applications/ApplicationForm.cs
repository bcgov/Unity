using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.GrantApplications;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicationForm : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid IntakeId { get; set; }
    [Required]
    public string? ApplicationFormName { get; set; }
    public string? ApplicationFormDescription { get; set; }
    public string? ChefsApplicationFormGuid { get; set; }
    public string? ChefsCriteriaFormGuid { get; set; }
    public string? ApiKey { get; set; }
    public string? AvailableChefsFields { get; set; }
    public int? Version { get; set; }
    public string? Category { get; set; }
    public string? ConnectionHttpStatus { get; set; }
    public DateTime? AttemptedConnectionDate { get; set; }
    public bool Payable {  get; set; }    
    public bool PreventPayment {  get; set; }
    public Guid? AccountCodingId { get; set; }
    public Guid? ScoresheetId {  get; set; }
    public Guid? TenantId { get; set; }
    public decimal? PaymentApprovalThreshold { get; set; }
    public int? DefaultPaymentGroup { get; set; }
    public FormHierarchyType? FormHierarchy { get; set; }
    public Guid? ParentFormId { get; set; }
    public bool IsDirectApproval { get; set; } = false;
    public List<ExternalLink> ExternalLinks { get; set; } = [];

    public bool AutomaticallyGenerateAIAnalysis { get; set; } = false;
    public bool ManuallyInitiateAIAnalysis { get; set; } = false;
    [MaxLength(100)]
    public string? Prefix { get; set; }
    public SuffixConfigType? SuffixType { get; set; }
    public static List<(SuffixConfigType SuffixType, string DisplayName)> GetAvailableSuffixTypes()
    {
        return [
            new (SuffixConfigType.SequentialNumber, "Sequential Number"),
            new (SuffixConfigType.SubmissionNumber, "Submission Number")
        ];
    }

    public ApplicationForm SetSuffixType(SuffixConfigType suffixType)
    {
        if (!Enum.IsDefined<SuffixConfigType>(suffixType))
        {
            throw new ArgumentOutOfRangeException(nameof(suffixType), "Invalid suffix type provided.");
        }
        SuffixType = suffixType;

        return this;
    }

    public AddressType? ElectoralDistrictAddressType { get; set; } = AddressType.PhysicalAddress;
    public static List<(AddressType AddressType, string DisplayName)> GetAvailableElectoralDistrictAddressTypes()
    {
        return [
            new (AddressType.PhysicalAddress, "Physical Address"),
            new (AddressType.MailingAddress, "Mailing Address")
        ];
    }

    public ApplicationForm SetElectoralDistrictAddressType(AddressType addressType)
    {
        if (!Enum.IsDefined<AddressType>(addressType))
        {
            throw new ArgumentOutOfRangeException(nameof(addressType), "Invalid address type provided.");
        }
        ElectoralDistrictAddressType = addressType;
        return this;
    }

    public static AddressType GetDefaultElectoralDistrictAddressType()
    {
        return AddressType.PhysicalAddress;
    }

    public const int MaxRelatedExternalLinks = 8;

    /// <summary>
    /// Replaces the Renewal and Related external links as a set, enforcing that a link
    /// cannot be marked visible in the Applicant Portal without a valid URI.
    /// </summary>
    public ApplicationForm SetExternalLinks(ExternalLink? renewalLink, List<ExternalLink> relatedLinks)
    {
        ArgumentNullException.ThrowIfNull(relatedLinks);

        // Cap the number of related links to the maximum allowed
        if (relatedLinks.Count > MaxRelatedExternalLinks)
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.TooManyRelatedLinks);
        }

        // Validate that if a renewal link is published, it must have a valid URI
        if (renewalLink is { Published: true } && string.IsNullOrWhiteSpace(renewalLink.Uri))
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.RenewalLinkRequiredForVisibility);
        }

        // Validate that if any related link is published, it must have a valid URI
        if (relatedLinks.Exists(l => l.Published && string.IsNullOrWhiteSpace(l.Uri)))
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.RelatedLinkInvalidUri);
        }

        var links = new List<ExternalLink>();

        if (renewalLink is not null)
        {
            renewalLink.ExternalLinkType = ExternalLinkType.Renewal;
            links.Add(renewalLink);
        }

        for (var i = 0; i < relatedLinks.Count; i++)
        {
            relatedLinks[i].ExternalLinkType = ExternalLinkType.Related;
            links.Add(relatedLinks[i]);
        }

        ExternalLinks = links;

        return this;
    }
}
