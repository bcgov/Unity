﻿using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;


namespace Unity.Payments.PaymentConfigurations
{
    public class PaymentConfiguration : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public decimal? PaymentThreshold { get; set; }
        public string? MinistryClient { get; private set; }
        public string? Responsibility { get; private set; }
        public string? ServiceLine { get; private set; }
        public string? Stob { get; private set; }
        public string? ProjectNumber { get; private set; }


        protected PaymentConfiguration()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public PaymentConfiguration(decimal? paymentThreshold,
            AccountCoding accountCoding)
        {
            PaymentThreshold = paymentThreshold;
            SetAccountCoding(accountCoding);
        }

        public void SetAccountCoding(AccountCoding accountCoding)
        {
            MinistryClient = accountCoding.MinistryClient;
            Responsibility = accountCoding.Responsibility;
            ServiceLine = accountCoding.ServiceLine;
            Stob = accountCoding.Stob;
            ProjectNumber = accountCoding.ProjectNumber;
        }
    }
}