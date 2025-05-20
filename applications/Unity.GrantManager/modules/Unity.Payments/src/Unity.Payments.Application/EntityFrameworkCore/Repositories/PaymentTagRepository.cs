﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentTags;
using Unity.Payments.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.Repositories
{
    public class PaymentTagRepository : EfCoreRepository<PaymentsDbContext, PaymentTag, Guid>, IPaymentTagRepository
    {
        public PaymentTagRepository(IDbContextProvider<PaymentsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
        public async Task<List<PaymentTag>> GetTagsByPaymentRequestIdAsync(Guid paymentRequestId)
        {
            var dbSet = await GetDbSetAsync();
            return dbSet.Where(p => p.PaymentRequestId.Equals(paymentRequestId)).ToList();
        }

        public virtual async Task<List<TagSummaryCount>> GetTagSummary()
        {
            var dbSet = await GetDbSetAsync();
            var results = dbSet
            .AsNoTracking()
            .AsEnumerable() // Forces client-side evaluation  
            .SelectMany(tag => tag.Text.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim()))
            .GroupBy(tag => tag)
            .Select(group => new TagSummaryCount(
                group.Key,
                group.Count()
            )).ToList();

            return results;
        }

        /// <summary>
        /// For a given Tag, finds the maximum length available for renaming.
        /// </summary>
        /// <param name="originalTag">The tag to be replaced.</param>
        /// <returns>The maximum length available for renaming</returns>
        public virtual async Task<int> GetMaxRenameLengthAsync(string originalTag)
        {
            var dbContext = await GetDbContextAsync();
            var entityType = dbContext.Model.FindEntityType(typeof(PaymentTag));
            var property = entityType?.FindProperty(nameof(PaymentTag.Text));

            int maxColumnLength = property?.GetMaxLength() ?? 0;

            var dbSet = await GetDbSetAsync();
            int? maxTagSetLength = await dbSet
                .AsNoTracking()
                .Where(t => t.Text.Contains(originalTag))
                .Select(t => t.Text.Length)
                .OrderByDescending(len => len)
                .FirstOrDefaultAsync();

            if (maxTagSetLength == null || maxTagSetLength == 0)
            {
                return maxColumnLength;
            }

            return maxColumnLength + originalTag.Length - maxTagSetLength.Value;
        }
    }
}
