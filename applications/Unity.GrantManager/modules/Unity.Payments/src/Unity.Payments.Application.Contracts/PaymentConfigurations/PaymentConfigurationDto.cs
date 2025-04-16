using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.PaymentConfigurations
{
    [Serializable]
    public class PaymentConfigurationDto : ExtensibleFullAuditedEntityDto<Guid>
    {        
        public decimal PaymentThreshold { get; set; }
        public string PaymentIdPrefix { get; set; } = string.Empty;
    }
}
