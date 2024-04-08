using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications
{
    public class ApplicationPaymentRequestDto : EntityDto<Guid>
    {
        public Guid ApplicationId { get; set; }
        public decimal? Amount { get; set; } 
        public string? Comment  { get; set; } = string.Empty;
        public string? InvoiceNumber  { get; set; } = string.Empty;
    }
}
