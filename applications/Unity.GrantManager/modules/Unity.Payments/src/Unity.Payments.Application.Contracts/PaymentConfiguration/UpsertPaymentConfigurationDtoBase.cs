using System;

namespace Unity.Payments.PaymentConfigurations
{
    [Serializable]
    public class UpsertPaymentConfigurationDtoBase
    {
        public decimal PaymentThreshold { get; set; }
        public string PaymentIdPrefix { get; set; } = string.Empty;
        public string MinistryClient { get; set; } = string.Empty;
        public string Responsibility { get; set; } = string.Empty;
        public string ServiceLine { get; set; } = string.Empty;
        public string Stob { get; set; } = string.Empty;
        public string ProjectNumber { get; set; } = string.Empty;
    }
}
