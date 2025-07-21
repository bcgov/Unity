using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.PaymentConfigurations
{
    [Serializable]
    public class PaymentConfigurationDto : ExtensibleFullAuditedEntityDto<Guid>
    {
        public string PaymentIdPrefix { get; set; } = string.Empty;
        public Guid DefaultAccountCodingId  { get; set; }
    }
}
