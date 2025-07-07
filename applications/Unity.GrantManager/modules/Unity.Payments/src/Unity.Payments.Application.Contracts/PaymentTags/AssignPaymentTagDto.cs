using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.PaymentTags;

[Serializable]
public class AssignPaymentTagDto : AuditedEntityDto<Guid>
{
    public Guid PaymentRequestId { get; set; }
   
    public List<GlobalTagDto>? Tags { get; set; }
}
