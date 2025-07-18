﻿using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.PaymentTags;

[Serializable]
public class PaymentTagDto : AuditedEntityDto<Guid>
{
    public Guid PaymentRequestId { get; set; }
    public GlobalTagDto? Tag { get; set; }
}
