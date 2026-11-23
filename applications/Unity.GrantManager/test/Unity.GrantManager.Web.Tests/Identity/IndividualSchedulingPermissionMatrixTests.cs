using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using Shouldly;
using Unity.AI.Permissions;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.Repositories;
using Unity.GrantManager.Web.Pages.Identity.Roles;
using Unity.Identity.Web.Pages.Identity.Users;
using Unity.Notifications.Features;
using Unity.Notifications.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.Identity;

public class IndividualSchedulingPermissionMatrixTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task Should_OnlyHideIndividualScheduling_ForTenantsWithTheFeatureDisabled(bool featureEnabled, bool isTenant)
    {
        string[] schedulingPermissions =
        [
            NotificationsPermissions.Email.Application.Schedule,
            NotificationsPermissions.Email.Application.CancelScheduled,
            NotificationsPermissions.Email.Applicant.Schedule,
            NotificationsPermissions.Email.Applicant.CancelScheduled
        ];
        string[] otherPermissions =
        [
            NotificationsPermissions.Email.Application.Send,
            NotificationsPermissions.Email.ScheduleCreate,
            NotificationsPermissions.Email.ScheduleCancel,
            MetabasePermissions.Access,
            AIPermissions.Reporting.ReportingDefault
        ];
        var names = schedulingPermissions.Concat(otherPermissions).ToArray();
        var definitions = names.Select(name => (PermissionDefinition)new TestPermissionDefinition(name)).ToArray();
        var definitionManager = Substitute.For<IPermissionDefinitionManager>();
        definitionManager.GetPermissionsAsync().Returns((IReadOnlyList<PermissionDefinition>)definitions);
        definitions.Single(permission => permission.Name == MetabasePermissions.Access).RequireFeatures("Unity.Reporting");
        definitions.Single(permission => permission.Name == AIPermissions.Reporting.ReportingDefault).RequireFeatures("Unity.AIReporting");
        var features = Substitute.For<IFeatureChecker>();
        features.IsEnabledAsync(Arg.Any<string>()).Returns(false);
        features.IsEnabledAsync(NotificationsFeatureConsts.IndividualScheduledNotification).Returns(featureEnabled);
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.IsAvailable.Returns(isTenant);
        tenant.Id.Returns(isTenant ? Guid.NewGuid() : (Guid?)null);
        var repository = Substitute.For<IPermissionRoleMatrixRepository>();
        repository.GetPermissionRoleMatrixAsync().Returns(names.Select(name => new PermissionRoleMatrixDto
        {
            GroupName = NotificationsPermissions.GroupName,
            PermissionName = name,
            PermissionDisplayName = name,
            RolePermissions = new Dictionary<string, bool> { ["manager"] = true }
        }).ToList());
        repository.GetPermissionUserMatrixAsync().Returns(new PermissionUserMatrixResult
        {
            Rows = names.Select(name => new PermissionUserMatrixRowDto
            {
                GroupName = NotificationsPermissions.GroupName,
                PermissionName = name,
                PermissionDisplayName = name,
                UserPermissions = []
            }).ToList()
        });
        var roles = new PermissionRoleMatrixModel(repository, definitionManager, features, tenant)
        {
            PermissionRoleMatrix = [],
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };
        var users = new PermissionUserMatrixModel(repository, definitionManager, features, tenant)
        {
            PermissionUserMatrix = [],
            Users = [],
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };

        await roles.OnGetAsync();
        await users.OnGetAsync();

        var expectedNames = !isTenant || featureEnabled ? names : otherPermissions;
        roles.PermissionRoleMatrix.Select(row => row.PermissionName).ShouldBe(expectedNames);
        users.PermissionUserMatrix.Select(row => row.PermissionName).ShouldBe(expectedNames);
        roles.PermissionRoleMatrix.ShouldAllBe(row => row.IsDefined && row.RolePermissions["manager"]);
        users.PermissionUserMatrix.ShouldAllBe(row => row.IsDefined);
    }

    private sealed class TestPermissionDefinition(string name) : PermissionDefinition(name)
    {
    }
}
