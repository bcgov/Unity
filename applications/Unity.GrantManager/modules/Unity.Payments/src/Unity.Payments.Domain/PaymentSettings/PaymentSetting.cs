using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;


namespace Unity.Payments.PaymentSettings
{
    public class PaymentSetting : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public decimal? PaymentThreshold { get; set; }
        public string? MinistryClient { get; set; }
        public string? Responsibility { get; set; }
        public string? ServiceLine { get; set; }
        public string? Stob { get; set; }
        public string? ProjectNumber { get; set; }


        public PaymentSetting()
        {

        }
    }
}
