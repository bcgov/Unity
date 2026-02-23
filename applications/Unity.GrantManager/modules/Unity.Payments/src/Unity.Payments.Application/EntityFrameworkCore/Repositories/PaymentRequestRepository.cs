using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Payments.Codes;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.EntityFrameworkCore;
using Unity.Payments.Enums;
using Unity.Payments.PaymentRequests;
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
            return await dbSet.CountAsync(s => s.CorrelationId == correlationId);
        }

        public async Task<int> GetPaymentRequestCountBySiteId(Guid siteId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(s => s.SiteId == siteId)
                              .CountAsync();
        }

        public async Task<PaymentRequest?> GetPaymentRequestByInvoiceNumber(string invoiceNumber)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(s => s.InvoiceNumber == invoiceNumber)
                              .FirstOrDefaultAsync();
        }

        public async Task<decimal> GetTotalPaymentRequestAmountByCorrelationIdAsync(Guid correlationId)
        {
            var dbSet = await GetDbSetAsync();
            decimal applicationPaymentRequestsTotal = await dbSet
              .Where(p => p.CorrelationId.Equals(correlationId))
              .Where(p => p.Status != PaymentRequestStatus.L1Declined
                        && p.Status != PaymentRequestStatus.L2Declined
                        && p.Status != PaymentRequestStatus.L3Declined
                        && p.InvoiceStatus != CasPaymentRequestStatus.NotFound
                        && p.InvoiceStatus != CasPaymentRequestStatus.ErrorFromCas)
              .GroupBy(p => p.CorrelationId)
              .Select(p => p.Sum(q => q.Amount))
              .FirstOrDefaultAsync();

            return applicationPaymentRequestsTotal;
        }

        public async Task<List<PaymentRequest>> GetPaymentRequestsBySentToCasStatusAsync()
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(p => p.InvoiceStatus != null && ReCheckStatusList.Contains(p.InvoiceStatus))
                              .IncludeDetails()
                              .ToListAsync();
        }

        public async Task<List<PaymentRequest>> GetPaymentRequestsByFailedsStatusAsync()
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(p => p.InvoiceStatus != null
                                && FailedStatusList.Contains(p.InvoiceStatus)
                                && p.LastModificationTime >= DateTime.Now.AddDays(-2)).IncludeDetails().ToListAsync();
        }

        public override async Task<IQueryable<PaymentRequest>> WithDetailsAsync()
        {
            // Uses the extension method defined above
            return (await GetQueryableAsync()).IncludeDetails();
        }

        public async Task<List<PaymentRequest>> GetPaymentPendingListByCorrelationIdAsync(Guid correlationId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(p => p.CorrelationId.Equals(correlationId))
                        .Where(p => p.Status == PaymentRequestStatus.L1Pending || p.Status == PaymentRequestStatus.L2Pending)
                        .IncludeDetails()
                        .ToListAsync();
        }

        /// <summary>
        /// Asynchronously retrieves payment rollup information for each specified correlation ID.
        /// </summary>
        /// <remarks>This method queries the database for payment records associated with the provided
        /// correlation IDs and aggregates payment amounts based on their status. Ensure that the correlation IDs are
        /// valid to avoid empty results.</remarks>
        /// <param name="correlationIds">A list of correlation IDs used to filter payment records. Each ID must be a valid GUID.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of
        /// ApplicationPaymentRollupDto objects, each summarizing the total paid and total pending amounts for the
        /// corresponding correlation ID.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance",
            "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons",
            Justification = "EF Core does not support StringComparison - https://github.com/dotnet/efcore/issues/1222")]
        public async Task<List<ApplicationPaymentRollupDto>> GetBatchPaymentRollupsByCorrelationIdsAsync(List<Guid> correlationIds)
        {
            var dbSet = await GetDbSetAsync();

            var results = await dbSet
                .Where(p => correlationIds.Contains(p.CorrelationId))
                .GroupBy(p => p.CorrelationId)
                .Select(g => new ApplicationPaymentRollupDto
                {
                    ApplicationId = g.Key,
                    TotalPaid = g
                        .Where(p => p.PaymentStatus != null
                            && p.PaymentStatus.Trim().ToUpper() == CasPaymentRequestStatus.FullyPaid.ToUpper())
                        .Sum(p => p.Amount),
                    TotalPending = g
                        .Where(p => p.Status == PaymentRequestStatus.L1Pending
                            || p.Status == PaymentRequestStatus.L2Pending
                            || p.Status == PaymentRequestStatus.L3Pending
                            || (p.Status == PaymentRequestStatus.Submitted
                                && string.IsNullOrEmpty(p.PaymentStatus)
                                && (string.IsNullOrEmpty(p.InvoiceStatus)
                                    || !p.InvoiceStatus.Contains(CasPaymentRequestStatus.ErrorFromCas))))
                        .Sum(p => p.Amount)
                })
                .ToListAsync();

            return results;
        }
    }
}
