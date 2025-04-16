using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Payments
{
    public class AccountCodingDto : AuditedEntityDto<Guid>
{       
        public string? MinistryClient { get; private set; } = string.Empty;
        public string? Responsibility { get; private set; } = string.Empty;
        public string? ServiceLine { get; private set; } = string.Empty;
        public string? Stob { get; private set; } = string.Empty;
        public string? ProjectNumber { get; private set; } = string.Empty;
    }
}
