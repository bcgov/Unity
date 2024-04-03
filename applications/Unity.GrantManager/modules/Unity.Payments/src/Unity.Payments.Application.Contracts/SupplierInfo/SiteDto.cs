using System;
using Unity.Payments.Enums;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.SupplierInfo
{
    [Serializable]
    public class SiteDto
    {
        public string? MailingAddress { get; set; }
        public string?  Number { get; set; }
    }
}
