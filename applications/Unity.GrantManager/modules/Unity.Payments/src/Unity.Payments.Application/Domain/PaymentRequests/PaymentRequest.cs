﻿using System;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Enums;
using System.Collections.ObjectModel;
using System.Linq;
using Volo.Abp;
using Unity.Payments.Domain.Exceptions;
using Unity.Payments.PaymentRequests;
using Unity.Payments.Domain.PaymentTags;

namespace Unity.Payments.Domain.PaymentRequests
{
    public class PaymentRequest : FullAuditedAggregateRoot<Guid>, IMultiTenant, ICorrelationEntity
    {
        public Guid? TenantId { get; set; }
        public virtual Guid SiteId { get; set; }
        public virtual Site Site
        {
            set => _site = value;
            get => _site
                   ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Site));
        }
        private Site? _site;

        public virtual string InvoiceNumber { get; private set; } = string.Empty;
        public virtual decimal Amount { get; private set; }
        public virtual PaymentRequestStatus Status { get; private set; } = PaymentRequestStatus.L1Pending;
        public virtual string? Description { get; private set; } = null;

        public virtual bool IsRecon { get; internal set; }

        public virtual string ReferenceNumber { get;  set; } = string.Empty;
        public virtual string SubmissionConfirmationCode { get; set; } = string.Empty;

        // Filled on a recon
        public virtual string? InvoiceStatus { get; private set; }
        public virtual string? PaymentStatus { get; private set; }
        public virtual string? PaymentNumber { get; private set; }
        public virtual string? PaymentDate { get; private set; }

        // External Correlation
        public virtual Guid CorrelationId { get; private set; }
        public virtual string CorrelationProvider { get; private set; } = string.Empty;
        // Payee Info
        public virtual string PayeeName { get; private set; } = string.Empty;
        public virtual string ContractNumber { get; private set; } = string.Empty;
        public virtual string? SupplierName { get; private set; } = string.Empty;
        public virtual string SupplierNumber { get; private set; } = string.Empty;
        public virtual string RequesterName { get; private set; } = string.Empty;
        public virtual string BatchName { get; private set; } = string.Empty;
        public virtual decimal BatchNumber { get; private set; } = 0;
        public virtual Collection<PaymentTag>? PaymentTags { get; set; } 
        public virtual Collection<ExpenseApproval> ExpenseApprovals { get; private set; }
        public virtual bool IsApproved { get => ExpenseApprovals.All(s => s.Status == ExpenseApprovalStatus.Approved); }

        // Corperate Accounting System
        public virtual int? CasHttpStatusCode { get; private set; } = null;
        public virtual string? CasResponse { get; private set; } = string.Empty;

        protected PaymentRequest()
        {
            ExpenseApprovals = [];
            PaymentTags = [];
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        private static Collection<ExpenseApproval> GenerateExpenseApprovals(decimal amount, decimal? paymentThreshold = 500000m)
        {
            var expenseApprovals = new Collection<ExpenseApproval>()
            {
                new(Guid.NewGuid(), ExpenseApprovalType.Level1),
                new(Guid.NewGuid(), ExpenseApprovalType.Level2)
            };

            if (amount >= paymentThreshold)
            {
                expenseApprovals.Add(new ExpenseApproval(Guid.NewGuid(), ExpenseApprovalType.Level3));
            }

            return expenseApprovals;
        }

        public PaymentRequest(Guid id, CreatePaymentRequestDto createPaymentRequestDto) : base(id)
        {
            InvoiceNumber = createPaymentRequestDto.InvoiceNumber;
            Amount = createPaymentRequestDto.Amount;
            PayeeName = createPaymentRequestDto.PayeeName;
            ContractNumber = createPaymentRequestDto.ContractNumber;
            SupplierNumber = createPaymentRequestDto.SupplierNumber;
            SupplierName = createPaymentRequestDto.SupplierName;
            SiteId = createPaymentRequestDto.SiteId;
            Description = createPaymentRequestDto.Description;
            CorrelationId = createPaymentRequestDto.CorrelationId;
            CorrelationProvider = createPaymentRequestDto.CorrelationProvider;
            ReferenceNumber = createPaymentRequestDto.ReferenceNumber;
            SubmissionConfirmationCode = createPaymentRequestDto.SubmissionConfirmationCode;
            BatchName = createPaymentRequestDto.BatchName;
            BatchNumber = createPaymentRequestDto.BatchNumber;
            PaymentTags = null;
            ExpenseApprovals = GenerateExpenseApprovals(createPaymentRequestDto.Amount, createPaymentRequestDto.PaymentThreshold);
            ValidatePaymentRequest();
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
            if (!string.IsNullOrEmpty(paymentDate)
                && DateTime.TryParseExact(paymentDate,
                                          "dd-MMM-yyyy",
                                          System.Globalization.CultureInfo.InvariantCulture,
                                          System.Globalization.DateTimeStyles.None,
                                          out DateTime date))
            {
                PaymentDate = date.ToString("yyyy-MM-dd");
            }
            else if(!string.IsNullOrEmpty(paymentDate))
            {
                PaymentDate = paymentDate;
            }
            return this;
        }

        public PaymentRequest SetCasHttpStatusCode(int casHttpStatusCode)
        {
            CasHttpStatusCode = casHttpStatusCode;
            return this;
        }

        public PaymentRequest SetCasResponse(string casResponse)
        {
            CasResponse = casResponse;
            return this;
        }

        public PaymentRequest ValidatePaymentRequest()
        {
            if (Amount <= 0)
            {
                throw new BusinessException(ErrorConsts.ZeroPayment);
            }


            return this;
        }
    }
}
