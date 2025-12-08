using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.PaymentRequests
{
    [Serializable]
    public class AccountCodingDto : AuditedEntityDto<Guid>
    {
        public string MinistryClient { get; private set; }
        public string Responsibility { get; private set; }
        public string ServiceLine { get; private set; }
        public string Stob { get; private set; }
        public string ProjectNumber { get; private set; }
        public string? Description { get; private set; }
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