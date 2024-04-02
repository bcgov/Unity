using System;

namespace Unity.Payments.Settings
{
    [Serializable]
    public class UpdatePaymentsSettingsDto
    {
        public decimal PaymentThreshold { get; set; }
    }
}
