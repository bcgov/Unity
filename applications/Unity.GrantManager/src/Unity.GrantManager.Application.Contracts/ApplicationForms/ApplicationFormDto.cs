using System;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.ApplicationForms
{
    public class ApplicationFormDto : EntityDto<Guid>
    {
        public Guid IntakeId { get; set; }
        public string? ApplicationFormName { get; set; } = string.Empty;
        public string? ApplicationFormDescription { get; set; }
        public string? ChefsApplicationFormGuid { get; set; }
        public string? ChefsFormVersionGuid { get; set; }
        public string? ChefsCriteriaFormGuid { get; set; }
        public string? ApiKey { get; set; }
        public string? SubmissionHeaderMapping { get; set; }
        public string? AvailableChefsFields { get; set; }
        public string? Category { get; set; }
        public int? Version { get; set; }
        public string? ApiToken { get; set; }
        public string? ConnectionHttpStatus { get; set; }
        public DateTime? AttemptedConnectionDate { get; set; }
        public bool Payable { get; set; }
        public bool PreventPayment { get; set; }
        public Guid AccountCodingId { get; set; }
        public bool RenderFormIoToHtml { get; set; }
        public Guid? ScoresheetId { get; set; }
        public Guid? TenantId { get; set; }
        public bool IsDirectApproval { get; set; }
        public AddressType? ElectoralDistrictAddressType { get; set; }
        public string? Prefix { get; set; }
        public SuffixConfigType? SuffixType { get; set; }
        public int? DefaultPaymentGroup { get; set; }
    }
}
