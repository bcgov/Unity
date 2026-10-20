using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Integrations.Metabase;

/// <summary>
/// Queues a <see cref="MetabaseMemberRemovalJob"/> when a tenant user loses their claim to the
/// tenant's Metabase group: deleted (how staff are off-boarded), deactivated, or given a new email
/// by Rename (the old email's membership goes; the next sync adds the new one where it applies).
/// Only for tenants set up in Metabase.
/// </summary>
public class MetabaseMemberOffboardingHandler(
    IBackgroundJobManager backgroundJobManager,
    ITenantRepository tenantRepository,
    ICurrentTenant currentTenant,
    IOptions<MetabaseOptions> metabaseOptions)
    : ILocalEventHandler<EntityDeletedEventData<IdentityUser>>,
      ILocalEventHandler<EntityUpdatedEventData<IdentityUser>>,
      IDistributedEventHandler<IdentityUserEmailChangedEto>,
      ITransientDependency
{
    public Task HandleEventAsync(EntityDeletedEventData<IdentityUser> eventData) =>
        QueueRemovalAsync(eventData.Entity.TenantId, eventData.Entity.Email);

    public Task HandleEventAsync(EntityUpdatedEventData<IdentityUser> eventData) =>
        eventData.Entity.IsActive
            ? Task.CompletedTask
            : QueueRemovalAsync(eventData.Entity.TenantId, eventData.Entity.Email);

    public Task HandleEventAsync(IdentityUserEmailChangedEto eventData) =>
        QueueRemovalAsync(eventData.TenantId, eventData.OldEmail);

    private async Task QueueRemovalAsync(Guid? tenantId, string? email)
    {
        if (!metabaseOptions.Value.Enabled || tenantId == null || string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        Tenant? tenant;
        using (currentTenant.Change(null))
        {
            tenant = await tenantRepository.FindAsync(tenantId.Value);
        }
        if (tenant?.GetProperty<int?>(MetabaseTenantProvisioner.GroupIdProperty) == null)
        {
            return;
        }

        await backgroundJobManager.EnqueueAsync(new MetabaseMemberRemovalJobArgs
        {
            TenantId = tenantId.Value,
            Email = email
        });
    }
}
