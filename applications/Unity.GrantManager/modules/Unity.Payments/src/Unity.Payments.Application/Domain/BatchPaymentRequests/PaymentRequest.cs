﻿using System;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Enums;

namespace Unity.Payments.Domain.BatchPaymentRequests
{
    public class PaymentRequest : FullAuditedEntity<Guid>, IMultiTenant, ICorrelationIdEntity
    {
        public Guid? TenantId { get; set; }
        public virtual Guid? SiteId { get; set; }
        public virtual Site? Site { get; set; }
        public virtual string InvoiceNumber { get; private set; } = string.Empty;
        public virtual decimal Amount { get; private set; }
        public virtual PaymentRequestStatus Status { get; private set; } = PaymentRequestStatus.Created;
        public virtual string? Description { get; private set; } = null;
        public virtual BatchPaymentRequest? BatchPaymentRequest { get; set; }
        public virtual Guid BatchPaymentRequestId { get; set; }
        public virtual bool IsRecon { get; internal set; }

        // Filled on a recon
        public virtual string? InvoiceStatus { get; private set; }
        public virtual string? PaymentStatus { get; private set; }
        public virtual string? PaymentNumber { get; private set; }
        public virtual string? PaymentDate { get; private set; }

        // External Correlation
        public virtual Guid CorrelationId { get; private set; }

        // Payee Info
        public virtual string PayeeName { get; private set; } = string.Empty;
        public virtual string ContractNumber { get; private set; } = string.Empty;
        public virtual string SupplierNumber  { get; private set; } = string.Empty;

        protected PaymentRequest()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public PaymentRequest(Guid id,
            BatchPaymentRequest batch,
            string invoiceNumber,
            decimal amount,
            string payeeName,
            string contractNumber,
            string supplierNumber,
            Guid siteId,
            Guid correlationId,
            string? description = null)
            : base(id)
        {
            InvoiceNumber = invoiceNumber;
            Amount = amount;
            PayeeName = payeeName;
            ContractNumber = contractNumber;
            SupplierNumber = supplierNumber;
            SiteId = siteId;
            Description = description;
            BatchPaymentRequest = batch;
            CorrelationId = correlationId;
        }

        public PaymentRequest SetAmount(decimal amount)
        {
            Amount = amount;
            return this;
        }

        public PaymentRequest SetComment(string comment)
        {
            Description = comment;
            return this;
        }

        public PaymentRequest SetPaymentRequestStatus(PaymentRequestStatus status)
        {
            Status = status;
            return this;
        }

        public PaymentRequest SetInvoiceStatus(string status)
        {
            InvoiceStatus = status;
            return this;
        }

        public PaymentRequest SetPaymentStatus(string status)
        {
            PaymentStatus = status;
            return this;
        }

        public PaymentRequest SetPaymentNumber(string paymentNumber)
        {
            PaymentNumber = paymentNumber;
            return this;
        }

        public PaymentRequest SetPaymentDate(string paymentDate)
        {
            PaymentDate = paymentDate;
            return this;
        }
    }
}