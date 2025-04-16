using System;

namespace Unity.Payments.PaymentConfigurations
{
    [Serializable]
    public class UpsertPaymentConfigurationDtoBase
    {
        public decimal PaymentThreshold { get; set; }
        public string PaymentIdPrefix { get; set; } = string.Empty;
    }
}
