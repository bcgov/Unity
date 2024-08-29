namespace Unity.Payments.PaymentRequests
{
    public class PaymentRequestBackgroundJobsOptions
    {
        public bool IsJobExecutionEnabled { get; set; }
        public PaymentRequestQuartzBackgroundJobOptions Quartz { get; set; } = new();
        public PaymentRequestOptions PaymentRequestOptions { get; set; } = new();
        public FinancialNotificationSummaryOptions FinancialNotificationSummaryOptions { get; set; } = new();
    }

    public class PaymentRequestQuartzBackgroundJobOptions
    {
        public bool IsAutoRegisterEnabled { get; set; }
    }

    public class PaymentRequestOptions
    {
        public string ProducerExpression { get; set; } = string.Empty;
    }

    public class FinancialNotificationSummaryOptions
    {
        public string ProducerExpression { get; set; } = string.Empty;
    }
}