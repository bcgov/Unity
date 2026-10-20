using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Integrations.Metabase;

public class MetabaseMemberRemovalJobArgs
{
    public Guid TenantId { get; set; }

    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Removes one email from a tenant's Metabase group straight after the user is off-boarded or
/// their email changes (queued by <see cref="MetabaseMemberOffboardingHandler"/>), unless Unity
/// still wants it there. Metabase errors are rethrown so ABP's background-job retry runs; the next
/// members sync is the safety net for anything this misses.
/// </summary>
public class MetabaseMemberRemovalJob(
    MetabaseTenantProvisioner provisioner,
    IOptions<MetabaseOptions> metabaseOptions,
    ILogger<MetabaseMemberRemovalJob> logger)
    : AsyncBackgroundJob<MetabaseMemberRemovalJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(MetabaseMemberRemovalJobArgs args)
    {
        if (!metabaseOptions.Value.Enabled)
        {
            return;
        }

        // A tenant deleted, or never set up in Metabase, has no group to remove from.
        var groupId = await provisioner.FindGroupIdAsync(args.TenantId);
        if (groupId == null)
        {
            return;
        }

        var removed = await provisioner.RemoveMemberIfNotDesiredAsync(args.TenantId, groupId.Value, args.Email);

        // Not who: email addresses don't belong in logs.
        logger.LogInformation(
            "[Metabase] Off-boarding check for tenant {TenantId}: membership {Outcome}.",
            args.TenantId, removed ? "removed" : "unchanged");
    }
}
