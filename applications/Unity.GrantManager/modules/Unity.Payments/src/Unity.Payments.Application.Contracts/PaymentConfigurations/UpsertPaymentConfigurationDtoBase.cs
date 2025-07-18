using System;

namespace Unity.Payments.PaymentConfigurations
{
    [Serializable]
    public class UpsertPaymentConfigurationDtoBase
    {
        public string PaymentIdPrefix { get; set; } = string.Empty;
        public Guid DefaultAcountCodingId { get; set; }
    }
}
