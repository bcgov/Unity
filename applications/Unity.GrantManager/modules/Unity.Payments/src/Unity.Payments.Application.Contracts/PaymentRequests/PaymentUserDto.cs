using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.PaymentRequests;

public class PaymentUserDto : EntityDto<Guid>
{
    public string UserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
