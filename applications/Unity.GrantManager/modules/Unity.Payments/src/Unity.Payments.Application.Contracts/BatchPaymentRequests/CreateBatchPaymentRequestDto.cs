using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.BatchPaymentRequests
{
#pragma warning disable CS8618
    [Serializable]
    public class CreateBatchPaymentRequestDto
    {        
        public string? Description { get; set; }  
        public string Provider { get; set; }
        public List<PaymentRequestDto> Payments { get; set; } 
    }
#pragma warning restore CS8618
}
