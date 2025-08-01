﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Payments.Codes;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.EntityFrameworkCore;
using Unity.Payments.Enums;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.Repositories
{
    public class PaymentRequestRepository : EfCoreRepository<PaymentsDbContext, PaymentRequest, Guid>, IPaymentRequestRepository
    {
        private List<string> ReCheckStatusList { get; set; } = new List<string>();
        private List<string> FailedStatusList { get; set; } = new List<string>();




        public PaymentRequestRepository(IDbContextProvider<PaymentsDbContext> dbContextProvider) : base(dbContextProvider)
        {
            ReCheckStatusList.Add(CasPaymentRequestStatus.ServiceUnavailable);
            ReCheckStatusList.Add(CasPaymentRequestStatus.SentToCas);
            ReCheckStatusList.Add(CasPaymentRequestStatus.NeverValidated);

            FailedStatusList.Add(CasPaymentRequestStatus.ServiceUnavailable);
            FailedStatusList.Add(CasPaymentRequestStatus.ErrorFromCas);
        }

        public async Task<int> GetCountByCorrelationId(Guid correlationId)
        {
            var dbSet = await GetDbSetAsync();
            return dbSet.Count(s => s.CorrelationId == correlationId);
        }

        public async Task<int> GetPaymentRequestCountBySiteId(Guid siteId)
        {
            var dbSet = await GetDbSetAsync();
            return dbSet.Where(s => s.SiteId == siteId).Count();
        }   

        public async Task<PaymentRequest?> GetPaymentRequestByInvoiceNumber(string invoiceNumber)
        {
            var dbSet = await GetDbSetAsync();
            return dbSet.Where(s => s.InvoiceNumber == invoiceNumber).FirstOrDefault();
        }

        public async Task<decimal> GetTotalPaymentRequestAmountByCorrelationIdAsync(Guid correlationId)
        {
            var dbSet = await GetDbSetAsync();
            decimal applicationPaymentRequestsTotal = dbSet
              .Where(p => p.CorrelationId.Equals(correlationId))
              .Where(p => p.Status != PaymentRequestStatus.L1Declined
                        && p.Status != PaymentRequestStatus.L2Declined
                        && p.Status != PaymentRequestStatus.L3Declined
                        && p.InvoiceStatus != CasPaymentRequestStatus.ErrorFromCas)
              .GroupBy(p => p.CorrelationId)
              .Select(p => p.Sum(q => q.Amount))
              .FirstOrDefault();

            return applicationPaymentRequestsTotal;
        }

        public async Task<List<PaymentRequest>> GetPaymentRequestsBySentToCasStatusAsync()
        {
            var dbSet = await GetDbSetAsync();
            return dbSet.Where(p => p.InvoiceStatus != null && ReCheckStatusList.Contains(p.InvoiceStatus)).IncludeDetails().ToList();
        }

        public async Task<List<PaymentRequest>> GetPaymentRequestsByFailedsStatusAsync()
        {
            var dbSet = await GetDbSetAsync();
            return dbSet.Where(p => p.InvoiceStatus != null 
                                && FailedStatusList.Contains(p.InvoiceStatus) 
                                && p.LastModificationTime >= DateTime.Now.AddDays(-2)).IncludeDetails().ToList();
        }

        public override async Task<IQueryable<PaymentRequest>> WithDetailsAsync()
        {
            // Uses the extension method defined above
            return (await GetQueryableAsync()).IncludeDetails();
        }

        public async Task<List<PaymentRequest>> GetPaymentPendingListByCorrelationIdAsync(Guid correlationId)
        {
            var dbSet = await GetDbSetAsync();
            return dbSet.Where(p => p.CorrelationId.Equals(correlationId))
                        .Where(p => p.Status == PaymentRequestStatus.L1Pending || p.Status == PaymentRequestStatus.L2Pending).IncludeDetails().ToList();
        }
    }
}
