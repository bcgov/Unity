using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.PermissionManagement;
using Xunit;

namespace Unity.GrantManager.Domain.Tests.Permissions;

public class StatusWorkflowPermissionDefaultsTests
{
    private static readonly Dictionary<string, string[]> ExpectedRoles = new()
    {
        [GrantApplicationPermissions.StatusWorkflow.Default] = [UnityRoles.ProgramManager, UnityRoles.TeamLead,
            UnityRoles.Reviewer, UnityRoles.ExternalAssessor, UnityRoles.Assessor, UnityRoles.Approver],
        [GrantApplicationPermissions.Assignments.AssignInitial] = [UnityRoles.ProgramManager, UnityRoles.TeamLead,
            UnityRoles.Reviewer, UnityRoles.ExternalAssessor, UnityRoles.Assessor, UnityRoles.Approver],
        [GrantApplicationPermissions.Reviews.StartInitial] = [UnityRoles.ProgramManager, UnityRoles.TeamLead, UnityRoles.Reviewer],
        [GrantApplicationPermissions.Reviews.CompleteInitial] = [UnityRoles.ProgramManager, UnityRoles.TeamLead, UnityRoles.Reviewer],
        [GrantApplicationPermissions.Assessments.Start] = [UnityRoles.ProgramManager, UnityRoles.TeamLead,
            UnityRoles.ExternalAssessor, UnityRoles.Assessor],
        [GrantApplicationPermissions.Assessments.Complete] = [UnityRoles.ProgramManager, UnityRoles.TeamLead,
            UnityRoles.ExternalAssessor, UnityRoles.Assessor],
        [GrantApplicationPermissions.Approvals.Complete] = [UnityRoles.Approver],
        [GrantApplicationPermissions.Approvals.DeferAfterApproval] = [UnityRoles.TeamLead, UnityRoles.Approver],
        [GrantApplicationPermissions.Approvals.BulkApplicationApproval] = [UnityRoles.TeamLead]
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_Seed_The_Requested_Default_Role_Lists(bool isTenant)
    {
        var fixture = new SeedFixture(isTenant ? Guid.NewGuid() : null);

        await fixture.SeedAsync();

        foreach (var (permission, roles) in ExpectedRoles)
        {
            fixture.Assignments.Where(role => role.Value.Contains(permission)).Select(role => role.Key).Order()
                .ShouldBe(roles.Order());
        }
        fixture.Requests.Select(request => request.Role).Order()
            .ShouldBe(UnityRoles.DefinedRoles.Where(role => role != UnityRoles.FinancialAnalyst).Order());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_Restore_Removed_Defaults_And_Preserve_Additional_Permissions_On_Reseeding(bool isTenant)
    {
        var fixture = new SeedFixture(isTenant ? Guid.NewGuid() : null);
        await fixture.SeedAsync();
        fixture.Assignments[UnityRoles.Assessor].UnionWith(
        [
            GrantApplicationPermissions.Reviews.StartInitial,
            GrantApplicationPermissions.Reviews.CompleteInitial,
            GrantApplicationPermissions.Approvals.Complete,
            "Custom.Permission"
        ]);
        fixture.Assignments[UnityRoles.Reviewer].Add(GrantApplicationPermissions.Assessments.Complete);
        fixture.Assignments["tenant_custom_role"] = ["Custom.RolePermission"];
        var expected = fixture.Assignments.ToDictionary(role => role.Key, role => role.Value.Order().ToArray());
        foreach (var (permission, roles) in ExpectedRoles)
        {
            foreach (var role in roles)
            {
                fixture.Assignments[role].Remove(permission);
            }
        }
        fixture.Requests.Clear();

        await fixture.SeedAsync();

        foreach (var (role, permissions) in expected)
        {
            fixture.Assignments[role].Order().ShouldBe(permissions);
        }
        fixture.Requests.Count.ShouldBe(10);
    }

    [Fact]
    public async Task Should_Leave_Retired_Assessor_Review_Grants_For_The_Migration_To_Remove()
    {
        var fixture = new SeedFixture(Guid.NewGuid());
        fixture.Assignments[UnityRoles.Assessor].UnionWith(
        [
            GrantApplicationPermissions.Reviews.StartInitial,
            GrantApplicationPermissions.Reviews.CompleteInitial
        ]);

        await fixture.SeedAsync();

        var requested = fixture.Requests.Single(request => request.Role == UnityRoles.Assessor).Permissions;
        requested.ShouldNotContain(GrantApplicationPermissions.Reviews.StartInitial);
        requested.ShouldNotContain(GrantApplicationPermissions.Reviews.CompleteInitial);
        fixture.Assignments[UnityRoles.Assessor].ShouldContain(GrantApplicationPermissions.Reviews.StartInitial);
        fixture.Assignments[UnityRoles.Assessor].ShouldContain(GrantApplicationPermissions.Reviews.CompleteInitial);
    }

    private sealed class SeedFixture
    {
        public Dictionary<string, HashSet<string>> Assignments { get; } =
            UnityRoles.DefinedRoles.ToDictionary(role => role, _ => new HashSet<string>());
        public List<(string Role, string[] Permissions)> Requests { get; } = new();
        private readonly Guid? _tenantId;
        private readonly PermissionGrantsDataSeeder _seeder;

        public SeedFixture(Guid? tenantId)
        {
            _tenantId = tenantId;
            var permissionSeeder = Substitute.For<IPermissionDataSeeder>();
            permissionSeeder.SeedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<Guid?>())
                .Returns(call =>
                {
                    call.ArgAt<string>(0).ShouldBe(RolePermissionValueProvider.ProviderName);
                    call.ArgAt<Guid?>(3).ShouldBe(tenantId);
                    var role = call.ArgAt<string>(1);
                    var permissions = call.ArgAt<IEnumerable<string>>(2).ToArray();
                    Requests.Add((role, permissions));
                    Assignments[role].UnionWith(permissions);
                    return Task.CompletedTask;
                });
            _seeder = new PermissionGrantsDataSeeder(permissionSeeder);
        }

        public Task SeedAsync() => _seeder.SeedAsync(new DataSeedContext(_tenantId));
    }
}
