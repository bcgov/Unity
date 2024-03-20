using System.Collections.Generic;

namespace Unity.Payments.BatchPaymentRequests
{
    public class CreatePaymentsBatchRequestDto
    {        
        public string? Description { get; set; }         
        public string Provider { get; set; } = string.Empty;
        public List<BatchPaymentDto> Payments { get; set; } = new List<BatchPaymentDto>();
    }
}
