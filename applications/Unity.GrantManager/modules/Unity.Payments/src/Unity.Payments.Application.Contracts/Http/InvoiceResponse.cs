using System;
using System.Net;
using System.Text.Json.Serialization;

namespace Unity.Payments.Integration.Http
{
	public class InvoiceResponse
	{
        [JsonPropertyName("invoice_number")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("CAS-Returned-Messages")]
        public string CASReturnedMessages { get; set; } = null!;

        public HttpStatusCode CASHttpStatusCode { get; set; }

        public bool IsSuccess() => "SUCCEEDED".Equals(CASReturnedMessages, StringComparison.OrdinalIgnoreCase);
	}
}