using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.PaymentRequests
{
    public class PaymentRequestListInputDto : PagedAndSortedResultRequestDto
    {
        public IReadOnlyList<string>? RequestedFields { get; set; }
        public DateTime? RequestedFromDate { get; set; }
        public DateTime? RequestedToDate { get; set; }
    }
}