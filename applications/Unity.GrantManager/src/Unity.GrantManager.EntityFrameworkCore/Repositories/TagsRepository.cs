using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GlobalTag;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.Applications;
using Unity.Payments.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Domain.ChangeTracking;

namespace Unity.GrantManager.Repositories;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ITagsRepository))]
public class TagsRepository
    : EfCoreRepository<GrantTenantDbContext, Tag, Guid>,
      ITagsRepository
{
    private readonly IDbContextProvider<PaymentsDbContext> _paymentDbContextProvider;
    private readonly ICurrentTenant _currentTenant;

    public TagsRepository(
        IDbContextProvider<GrantTenantDbContext> dbContextProvider,
        IDbContextProvider<PaymentsDbContext> paymentDbContextProvider,
        ICurrentTenant currentTenant
    ) : base(dbContextProvider)
    {
        _paymentDbContextProvider = paymentDbContextProvider;
        _currentTenant = currentTenant;
    }

    public virtual async Task<int> GetMaxRenameLengthAsync(string originalTag)
    {
        var dbContext = await GetDbContextAsync();
        var entityType = dbContext.Model.FindEntityType(typeof(Tag));
        var property = entityType?.FindProperty(nameof(Tag.Name));

        int maxColumnLength = property?.GetMaxLength() ?? 0;

        var dbSet = await GetDbSetAsync();
        int? maxTagSetLength = await dbSet
            .AsNoTracking()
            .Where(t => t.Name.Contains(originalTag))
            .Select(t => t.Name.Length)
            .OrderByDescending(len => len)
            .FirstOrDefaultAsync();

        if (maxTagSetLength == null || maxTagSetLength == 0)
        {
            return maxColumnLength;
        }

        return maxColumnLength + originalTag.Length - maxTagSetLength.Value;
    }

    [DisableEntityChangeTracking]
    public virtual async Task<List<TagUsageSummary>> GetTagUsageSummaryAsync()
    {
        var grantDbContext = await GetDbContextAsync();
        var tenantId = _currentTenant.Id;

        var tags = await grantDbContext.Tags
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .ToListAsync();

        var applicationTagCounts = await grantDbContext.ApplicationTags
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .GroupBy(x => x.TagId)
            .Select(g => new { TagId = g.Key, Count = g.Count() })
            .ToListAsync();

        var paymentDbContext = await _paymentDbContextProvider.GetDbContextAsync();

        var paymentTagCounts = await paymentDbContext.PaymentTags
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .GroupBy(x => x.TagId)
            .Select(g => new { TagId = g.Key, Count = g.Count() })
            .ToListAsync();

        var result = tags.Select(tag =>
        {
            var appCount = applicationTagCounts.FirstOrDefault(x => x.TagId == tag.Id)?.Count ?? 0;
            var payCount = paymentTagCounts.FirstOrDefault(x => x.TagId == tag.Id)?.Count ?? 0;

            return new TagUsageSummary
            {
                TagId = tag.Id,
                TagName = tag.Name,
                ApplicationTagCount = appCount,
                PaymentTagCount = payCount
            };
        }).ToList();

        return result;
    }
}
