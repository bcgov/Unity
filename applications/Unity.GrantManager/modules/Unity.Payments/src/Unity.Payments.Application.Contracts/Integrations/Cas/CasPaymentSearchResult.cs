using System.Text.Json.Serialization;

namespace Unity.Payments.Integrations.Cas
{
    public class CasPaymentSearchResult
    {
        [JsonPropertyName("invoice_number")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("invoice_status")]
        public string? InvoiceStatus { get; set; }

        [JsonPropertyName("payment_status")]
        public string? PaymentStatus { get; set; }

        [JsonPropertyName("payment_number")]
        public string? PaymentNumber { get; set; }

        [JsonPropertyName("payment_date")]
        public string? PaymentDate { get; set; }
    }
}