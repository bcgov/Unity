namespace Unity.Payments.PaymentRequests
{
    public class CasPaymentRequestBackgroundJobsOptions
    {
        public bool IsJobExecutionEnabled { get; set; }
        public PaymentRequestQuartzBackgroundJobOptions Quartz { get; set; } = new();
        public PaymentRequestOptions PaymentRequestOptions { get; set; } = new();
        public InvoiceRequestOptions InvoiceRequestOptions { get; set; } = new();
    }

    public class PaymentRequestQuartzBackgroundJobOptions
    {
        public bool IsAutoRegisterEnabled { get; set; }
    }

    public class PaymentRequestOptions
    {
        public string ConsumerExpression { get; set; } = string.Empty;
        public string ProducerExpression { get; set; } = string.Empty;
    }

    public class InvoiceRequestOptions
    {
        public string ConsumerExpression { get; set; } = string.Empty;
    }
}