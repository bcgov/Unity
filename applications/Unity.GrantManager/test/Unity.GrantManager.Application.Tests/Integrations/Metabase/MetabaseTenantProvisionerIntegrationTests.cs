using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.Integrations.Metabase;

/// <summary>
/// The provisioner as the container builds it, against the test database: proves the permission
/// grant repository resolves and the Metabase.Access query runs through EF in the tenant's context.
/// Only the Metabase HTTP client is faked.
/// </summary>
public class MetabaseTenantProvisionerIntegrationTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    private readonly IMetabaseApiClient _metabaseApiClient = Substitute.For<IMetabaseApiClient>();

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(_metabaseApiClient);
    }

    [Fact]
    public async Task GetMembersAsync_UserGrantedMetabaseAccess_ListedAsPermission()
    {
        var tenantId = Guid.NewGuid();
        var analystId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            using (GetRequiredService<ICurrentTenant>().Change(tenantId))
            {
                var users = GetRequiredService<IIdentityUserRepository>();
                await users.InsertAsync(new IdentityUser(analystId, "analyst", "analyst@gov.bc.ca", tenantId));
                await users.InsertAsync(new IdentityUser(otherId, "other", "other@gov.bc.ca", tenantId));

                var grants = GetRequiredService<IRepository<PermissionGrant, Guid>>();
                await grants.InsertAsync(new PermissionGrant(
                    Guid.NewGuid(), MetabasePermissions.Access, UserPermissionValueProvider.ProviderName, analystId.ToString(), tenantId));
                await grants.InsertAsync(new PermissionGrant(
                    Guid.NewGuid(), "GrantManagerManagement.Default", UserPermissionValueProvider.ProviderName, otherId.ToString(), tenantId));
            }
        });
        _metabaseApiClient.GetGroupMembersAsync(22).Returns(new List<MetabaseGroupMember>());

        var rows = await WithUnitOfWorkAsync(() =>
            GetRequiredService<MetabaseTenantProvisioner>().GetMembersAsync(tenantId, 22));

        var row = rows.ShouldHaveSingleItem();
        row.Email.ShouldBe("analyst@gov.bc.ca");
        row.Source.ShouldBe(MetabaseMemberSource.Permission);
        row.CanRemove.ShouldBeFalse();
    }
}
