using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.PaymentTags
{
    [Serializable]
    public class GlobalTagDto  : EntityDto<Guid>
    {
        public string Name { get; set; } = string.Empty;
    }
}

