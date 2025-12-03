using System;
using System.Collections.Generic;

namespace Unity.Payments.PaymentRequests
{
    public class StorePaymentIdsRequestDto
    {
        public List<Guid> PaymentRequestIds { get; set; } = new List<Guid>();
    }
}
