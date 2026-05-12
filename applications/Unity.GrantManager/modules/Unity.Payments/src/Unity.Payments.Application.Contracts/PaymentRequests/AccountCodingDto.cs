using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.PaymentRequests
{
    [Serializable]
    public class AccountCodingDto : AuditedEntityDto<Guid>
    {
        public string MinistryClient { get; init; }
        public string Responsibility { get; init; }
        public string ServiceLine { get; init; }
        public string Stob { get; init; }
        public string ProjectNumber { get; init; }
        public string? Description { get; init; }
        public AccountCodingDto()
        {
            MinistryClient = string.Empty;
            Responsibility = string.Empty;
            ServiceLine = string.Empty;
            Stob = string.Empty;
            ProjectNumber = string.Empty;
            Description = null;
        }
    }
}