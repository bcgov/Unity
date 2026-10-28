using System;
using System.Threading.Tasks;
using Shouldly;
using Unity.Notifications.EntityFrameworkCore;
using Unity.Notifications.Templates;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.Notifications;

public class TemplateLockRepositoryTests : NotificationsEntityFrameworkCoreTestBase
{
    [Fact]
    public async Task LockActiveAsync_ReturnsOnlyActiveTemplatesForCurrentTenant()
    {
        var repository = GetRequiredService<ITemplatesRepository>();
        var tenant = GetRequiredService<ICurrentTenant>();
        var tenantId = Guid.NewGuid();
        var template = new EmailTemplate(Guid.NewGuid(), "test", "", "", "", "", "") { TenantId = tenantId };

        using (tenant.Change(tenantId))
        {
            await WithUnitOfWorkAsync(() => repository.InsertAsync(template, autoSave: true));
            await WithUnitOfWorkAsync(async () =>
            {
                (await repository.LockActiveAsync(template.Id)).ShouldNotBeNull();
            });
        }

        using (tenant.Change(Guid.NewGuid()))
        {
            await WithUnitOfWorkAsync(async () =>
            {
                (await repository.LockActiveAsync(template.Id)).ShouldBeNull();
            });
        }

        using (tenant.Change(tenantId))
        {
            await WithUnitOfWorkAsync(() => repository.DeleteAsync(template.Id));
            await WithUnitOfWorkAsync(async () =>
            {
                (await repository.LockActiveAsync(template.Id)).ShouldBeNull();
            });
        }
    }
}
