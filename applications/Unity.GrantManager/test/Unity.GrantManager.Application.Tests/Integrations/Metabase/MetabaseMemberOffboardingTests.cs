using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Unity.GrantManager.Integrations.Exceptions;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities.Events;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Encryption;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Xunit;

namespace Unity.GrantManager.Integrations.Metabase;

public class MetabaseMemberOffboardingTests
{
    private static Tenant CreateTenant(int? groupId)
    {
        var ctor = typeof(Tenant).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null, [typeof(Guid), typeof(string), typeof(string)], null)!;
        var tenant = (Tenant)ctor.Invoke([Guid.NewGuid(), "AG-MARB", "AG-MARB"]);
        if (groupId != null)
        {
            tenant.SetProperty(MetabaseTenantProvisioner.GroupIdProperty, groupId.Value);
        }
        return tenant;
    }

    private static (MetabaseMemberOffboardingHandler Handler, IBackgroundJobManager Jobs) CreateHandler(
        Tenant tenant, bool enabled = true)
    {
        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.FindAsync(tenant.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(tenant);
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Change(Arg.Any<Guid?>(), Arg.Any<string?>()).Returns(Substitute.For<IDisposable>());
        var jobs = Substitute.For<IBackgroundJobManager>();

        var handler = new MetabaseMemberOffboardingHandler(
            jobs, tenantRepository, currentTenant, Options.Create(new MetabaseOptions { Enabled = enabled }));
        return (handler, jobs);
    }

    private static IdentityUser CreateUser(Guid? tenantId, string email, bool isActive = true)
    {
        var user = new IdentityUser(Guid.NewGuid(), email, email, tenantId);
        user.SetIsActive(isActive);
        return user;
    }

    private static Task ReceivedRemoval(IBackgroundJobManager jobs, Guid tenantId, string email) =>
        jobs.Received(1).EnqueueAsync(
            Arg.Is<MetabaseMemberRemovalJobArgs>(a => a.TenantId == tenantId && a.Email == email),
            Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());

    private static Task ReceivedNoJob(IBackgroundJobManager jobs) =>
        jobs.DidNotReceiveWithAnyArgs().EnqueueAsync<MetabaseMemberRemovalJobArgs>(default!);

    [Fact]
    public async Task UserDeleted_QueuesRemoval()
    {
        var tenant = CreateTenant(140);
        var (handler, jobs) = CreateHandler(tenant);

        await handler.HandleEventAsync(new EntityDeletedEventData<IdentityUser>(CreateUser(tenant.Id, "gone@gov.bc.ca")));

        await ReceivedRemoval(jobs, tenant.Id, "gone@gov.bc.ca");
    }

    [Fact]
    public async Task UserDeactivated_QueuesRemoval()
    {
        var tenant = CreateTenant(140);
        var (handler, jobs) = CreateHandler(tenant);

        await handler.HandleEventAsync(new EntityUpdatedEventData<IdentityUser>(CreateUser(tenant.Id, "off@gov.bc.ca", isActive: false)));

        await ReceivedRemoval(jobs, tenant.Id, "off@gov.bc.ca");
    }

    [Fact]
    public async Task ActiveUserUpdated_QueuesNothing()
    {
        var tenant = CreateTenant(140);
        var (handler, jobs) = CreateHandler(tenant);

        await handler.HandleEventAsync(new EntityUpdatedEventData<IdentityUser>(CreateUser(tenant.Id, "here@gov.bc.ca")));

        await ReceivedNoJob(jobs);
    }

    [Fact]
    public async Task EmailChanged_QueuesRemovalOfOldEmail()
    {
        var tenant = CreateTenant(140);
        var (handler, jobs) = CreateHandler(tenant);

        await handler.HandleEventAsync(new IdentityUserEmailChangedEto
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "new@gov.bc.ca",
            OldEmail = "old@gov.bc.ca"
        });

        await ReceivedRemoval(jobs, tenant.Id, "old@gov.bc.ca");
    }

    [Fact]
    public async Task TenantNotSetUpInMetabase_QueuesNothing()
    {
        var tenant = CreateTenant(groupId: null);
        var (handler, jobs) = CreateHandler(tenant);

        await handler.HandleEventAsync(new EntityDeletedEventData<IdentityUser>(CreateUser(tenant.Id, "gone@gov.bc.ca")));

        await ReceivedNoJob(jobs);
    }

    [Fact]
    public async Task MetabaseDisabled_QueuesNothing()
    {
        var tenant = CreateTenant(140);
        var (handler, jobs) = CreateHandler(tenant, enabled: false);

        await handler.HandleEventAsync(new EntityDeletedEventData<IdentityUser>(CreateUser(tenant.Id, "gone@gov.bc.ca")));

        await ReceivedNoJob(jobs);
    }

    [Fact]
    public async Task HostUserDeleted_QueuesNothing()
    {
        var tenant = CreateTenant(140);
        var (handler, jobs) = CreateHandler(tenant);

        await handler.HandleEventAsync(new EntityDeletedEventData<IdentityUser>(CreateUser(null, "host@gov.bc.ca")));

        await ReceivedNoJob(jobs);
    }

    private static MetabaseTenantProvisioner CreateProvisionerSubstitute() =>
        Substitute.For<MetabaseTenantProvisioner>(
            Substitute.For<IMetabaseApiClient>(), Substitute.For<ITenantRepository>(), Substitute.For<IIdentityUserRepository>(),
            Substitute.For<IStringEncryptionService>(), Substitute.For<ISettingManager>(), Substitute.For<ICurrentTenant>(),
            Substitute.For<IDataFilter>(), Options.Create(new MetabaseOptions()),
            Substitute.For<ILogger<MetabaseTenantProvisioner>>());

    private static MetabaseMemberRemovalJob CreateJob(MetabaseTenantProvisioner provisioner, bool enabled = true) =>
        new(provisioner, Options.Create(new MetabaseOptions { Enabled = enabled }), Substitute.For<ILogger<MetabaseMemberRemovalJob>>());

    [Fact]
    public async Task RemovalJob_TenantSetUp_RemovesUnlessDesired()
    {
        var tenantId = Guid.NewGuid();
        var provisioner = CreateProvisionerSubstitute();
        provisioner.FindGroupIdAsync(tenantId).Returns(140);

        await CreateJob(provisioner).ExecuteAsync(new MetabaseMemberRemovalJobArgs { TenantId = tenantId, Email = "gone@gov.bc.ca" });

        await provisioner.Received(1).RemoveMemberIfNotDesiredAsync(tenantId, 140, "gone@gov.bc.ca");
    }

    [Fact]
    public async Task RemovalJob_TenantGoneOrNotSetUp_DoesNothing()
    {
        var tenantId = Guid.NewGuid();
        var provisioner = CreateProvisionerSubstitute();
        provisioner.FindGroupIdAsync(tenantId).Returns((int?)null);

        await CreateJob(provisioner).ExecuteAsync(new MetabaseMemberRemovalJobArgs { TenantId = tenantId, Email = "gone@gov.bc.ca" });

        await provisioner.DidNotReceiveWithAnyArgs().RemoveMemberIfNotDesiredAsync(default, default, default!);
    }

    [Fact]
    public async Task RemovalJob_MetabaseError_Rethrows_SoAbpRetries()
    {
        var tenantId = Guid.NewGuid();
        var provisioner = CreateProvisionerSubstitute();
        provisioner.FindGroupIdAsync(tenantId).Returns(140);
        provisioner.RemoveMemberIfNotDesiredAsync(tenantId, 140, "gone@gov.bc.ca")
            .ThrowsAsync(new IntegrationServiceException("Metabase down"));

        await Should.ThrowAsync<IntegrationServiceException>(() =>
            CreateJob(provisioner).ExecuteAsync(new MetabaseMemberRemovalJobArgs { TenantId = tenantId, Email = "gone@gov.bc.ca" }));
    }
}
