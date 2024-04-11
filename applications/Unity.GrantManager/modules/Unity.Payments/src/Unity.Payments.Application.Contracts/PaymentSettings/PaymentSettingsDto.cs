namespace Unity.Payments.PaymentSettings
{
    public class PaymentSettingsDto
    {        
        public decimal? PaymentThreshold { get; set; }
        public string? MinistryClient { get; set; }
        public string? Responsibility { get; set; }
        public string? ServiceLine { get; set; }
        public string? Stob { get; set; }
        public string? ProjectNumber { get; set; }
    }
}
