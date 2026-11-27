using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Migrations.HostMigrations;
using Unity.GrantManager.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Xunit;

namespace Unity.GrantManager.Domain.Tests.Permissions;

public class StatusWorkflowPermissionSeedingIntegrationTests : GrantManagerDomainTestBase
{
    private static readonly string[] AssessorDefaults =
    [
        GrantApplicationPermissions.StatusWorkflow.Default,
        GrantApplicationPermissions.Assignments.AssignInitial,
        GrantApplicationPermissions.Assessments.Start,
        GrantApplicationPermissions.Assessments.Complete
    ];

    private static readonly string[] InitialReviewPermissions =
    [
        GrantApplicationPermissions.Reviews.StartInitial,
        GrantApplicationPermissions.Reviews.CompleteInitial
    ];

    private static readonly string[] ApprovalPermissions =
    [
        GrantApplicationPermissions.Approvals.Complete,
        GrantApplicationPermissions.Approvals.DeferAfterApproval,
        GrantApplicationPermissions.Approvals.BulkApplicationApproval
    ];

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        // These definitions support real permission-manager calls without referencing Application.Contracts.
        services.AddTransient<StatusWorkflowTestPermissionDefinitionProvider>();
        services.Configure<AbpPermissionOptions>(options =>
        {
            if (!options.DefinitionProviders.Contains(typeof(StatusWorkflowTestPermissionDefinitionProvider)))
            {
                options.DefinitionProviders.Add<StatusWorkflowTestPermissionDefinitionProvider>();
            }
        });
    }

    [Fact]
    public async Task Should_Persist_Current_Defaults_Without_Duplicating_Grants()
    {
        await SeedAsync();
        await SeedAsync();

        await WithUnitOfWorkAsync(async () =>
        {
            var assignments = await GetRequiredService<IPermissionGrantRepository>().GetListAsync(
                AssessorDefaults.Concat(InitialReviewPermissions).ToArray(),
                RolePermissionValueProvider.ProviderName, UnityRoles.Assessor);
            assignments.Select(assignment => assignment.Name).Order().ShouldBe(AssessorDefaults.Order());
        });
    }

    [Fact]
    public async Task Should_Restore_Missing_Defaults_For_Existing_Roles_After_The_Migration()
    {
        var repository = GetRequiredService<IPermissionGrantRepository>();
        var permissionStore = GetRequiredService<IPermissionStore>();
        await WithUnitOfWorkAsync(async () =>
        {
            await RemoveRolePermissionsAsync(UnityRoles.Reviewer, GrantApplicationPermissions.StatusWorkflow.Default,
                GrantApplicationPermissions.Assignments.AssignInitial, GrantApplicationPermissions.Reviews.StartInitial);
            await RemoveRolePermissionsAsync(UnityRoles.Assessor, GrantApplicationPermissions.StatusWorkflow.Default,
                GrantApplicationPermissions.Assignments.AssignInitial, GrantApplicationPermissions.Assessments.Start);
            await RemoveRolePermissionsAsync(UnityRoles.Approver, GrantApplicationPermissions.Approvals.Complete,
                GrantApplicationPermissions.Approvals.DeferAfterApproval);
            await AddRolePermissionsAsync(UnityRoles.Assessor, InitialReviewPermissions);
        });
        await WithUnitOfWorkAsync(async () =>
        {
            (await permissionStore.IsGrantedAsync(GrantApplicationPermissions.Assignments.AssignInitial,
                RolePermissionValueProvider.ProviderName, UnityRoles.Reviewer)).ShouldBeFalse();
        });

        await ApplyCorrectionAsync();
        await SeedAsync();

        await WithUnitOfWorkAsync(async () =>
        {
            var reviewerDefaults = InitialReviewPermissions.Concat(new[]
            {
                GrantApplicationPermissions.StatusWorkflow.Default,
                GrantApplicationPermissions.Assignments.AssignInitial
            }).ToArray();
            var reviewerAssignments = await repository.GetListAsync(reviewerDefaults,
                RolePermissionValueProvider.ProviderName, UnityRoles.Reviewer);
            reviewerAssignments.Select(assignment => assignment.Name).Order().ShouldBe(reviewerDefaults.Order());
            (await permissionStore.IsGrantedAsync(GrantApplicationPermissions.Assignments.AssignInitial,
                RolePermissionValueProvider.ProviderName, UnityRoles.Reviewer)).ShouldBeTrue();

            var assessorAssignments = await repository.GetListAsync(AssessorDefaults.Concat(InitialReviewPermissions).ToArray(),
                RolePermissionValueProvider.ProviderName, UnityRoles.Assessor);
            assessorAssignments.Select(assignment => assignment.Name).Order().ShouldBe(AssessorDefaults.Order());
            var approverAssignments = await repository.GetListAsync(ApprovalPermissions,
                RolePermissionValueProvider.ProviderName, UnityRoles.Approver);
            approverAssignments.Select(assignment => assignment.Name).Order().ShouldBe(new[]
            {
                GrantApplicationPermissions.Approvals.Complete,
                GrantApplicationPermissions.Approvals.DeferAfterApproval
            }.Order());
        });
    }

    [Fact]
    public async Task Should_Correct_Retired_Grants_And_Preserve_Additional_Role_And_User_Assignments()
    {
        var repository = GetRequiredService<IPermissionGrantRepository>();
        var tenantId = GetRequiredService<ICurrentTenant>().Id;
        var userId = Guid.NewGuid().ToString();
        var preservedPermissions = ApprovalPermissions.Append(GrantApplicationPermissions.Comments.Add).ToArray();
        await WithUnitOfWorkAsync(async () =>
        {
            await AddRolePermissionsAsync(UnityRoles.Assessor, InitialReviewPermissions.Concat(preservedPermissions).ToArray());
            await repository.InsertAsync(new PermissionGrant(Guid.NewGuid(), GrantApplicationPermissions.Reviews.StartInitial,
                UserPermissionValueProvider.ProviderName, userId, tenantId), autoSave: true);
        });

        await ApplyCorrectionAsync();
        await SeedAsync();

        await WithUnitOfWorkAsync(async () =>
        {
            var assignments = await repository.GetListAsync(InitialReviewPermissions.Concat(preservedPermissions).ToArray(),
                RolePermissionValueProvider.ProviderName, UnityRoles.Assessor);
            assignments.Select(assignment => assignment.Name).Order().ShouldBe(preservedPermissions.Order());
            (await GetRequiredService<IPermissionStore>().IsGrantedAsync(GrantApplicationPermissions.Reviews.StartInitial,
                UserPermissionValueProvider.ProviderName, userId)).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Should_Restore_Removed_Defaults_And_Keep_Admin_Additions_On_Subsequent_Runs()
    {
        await ApplyCorrectionAsync();
        var permissionManager = GetRequiredService<IPermissionManager>();
        await WithUnitOfWorkAsync(async () =>
        {
            await permissionManager.SetAsync(GrantApplicationPermissions.Assessments.Start,
                RolePermissionValueProvider.ProviderName, UnityRoles.Assessor, false);
            await permissionManager.SetAsync(GrantApplicationPermissions.Reviews.StartInitial,
                RolePermissionValueProvider.ProviderName, UnityRoles.Assessor, true);
            await permissionManager.SetAsync(GrantApplicationPermissions.Approvals.Complete,
                RolePermissionValueProvider.ProviderName, UnityRoles.Approver, false);
        });

        await SeedAsync();
        await SeedAsync();

        await WithUnitOfWorkAsync(async () =>
        {
            var repository = GetRequiredService<IPermissionGrantRepository>();
            var assignments = await repository.GetListAsync(AssessorDefaults.Concat(InitialReviewPermissions).ToArray(),
                RolePermissionValueProvider.ProviderName, UnityRoles.Assessor);
            assignments.Select(assignment => assignment.Name).Order()
                .ShouldBe(AssessorDefaults.Append(GrantApplicationPermissions.Reviews.StartInitial).Order());
            var approverAssignments = await repository.GetListAsync(ApprovalPermissions,
                RolePermissionValueProvider.ProviderName, UnityRoles.Approver);
            approverAssignments.ShouldContain(assignment => assignment.Name == GrantApplicationPermissions.Approvals.Complete);
        });
    }

    private Task ApplyCorrectionAsync() => WithUnitOfWorkAsync(async () =>
    {
        var dbContext = await GetRequiredService<IDbContextProvider<GrantManagerDbContext>>().GetDbContextAsync();
        foreach (var operation in new CorrectStatusWorkflowRoleDefaults().UpOperations.Cast<SqlOperation>())
        {
            await dbContext.Database.ExecuteSqlRawAsync(operation.Sql);
        }
    });

    private Task SeedAsync() => WithUnitOfWorkAsync(() => GetRequiredService<PermissionGrantsDataSeeder>()
        .SeedAsync(new DataSeedContext(GetRequiredService<ICurrentTenant>().Id)));

    private async Task AddRolePermissionsAsync(string roleName, params string[] permissions)
    {
        var repository = GetRequiredService<IPermissionGrantRepository>();
        var existing = await repository.GetListAsync(permissions, RolePermissionValueProvider.ProviderName, roleName);
        foreach (var permission in permissions.Except(existing.Select(assignment => assignment.Name)))
        {
            await repository.InsertAsync(new PermissionGrant(Guid.NewGuid(), permission,
                RolePermissionValueProvider.ProviderName, roleName, GetRequiredService<ICurrentTenant>().Id), autoSave: true);
        }
    }

    private async Task RemoveRolePermissionsAsync(string roleName, params string[] permissions)
    {
        var repository = GetRequiredService<IPermissionGrantRepository>();
        var assignments = await repository.GetListAsync(permissions, RolePermissionValueProvider.ProviderName, roleName);
        foreach (var assignment in assignments)
        {
            await repository.DeleteAsync(assignment, autoSave: true);
        }
    }
}

// Registered explicitly by these integration tests so other test hosts can use their own definitions.
[DisableConventionalRegistration]
public class StatusWorkflowTestPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var group = context.AddGroup(GrantApplicationPermissions.GroupName);
        var workflow = group.AddPermission(GrantApplicationPermissions.StatusWorkflow.Default);
        workflow.AddChild(GrantApplicationPermissions.Assignments.AssignInitial);
        workflow.AddChild(GrantApplicationPermissions.Reviews.StartInitial);
        workflow.AddChild(GrantApplicationPermissions.Reviews.CompleteInitial);
        workflow.AddChild(GrantApplicationPermissions.Assessments.Start);
        workflow.AddChild(GrantApplicationPermissions.Assessments.Complete);
        workflow.AddChild(GrantApplicationPermissions.Approvals.Complete);
        workflow.AddChild(GrantApplicationPermissions.Approvals.DeferAfterApproval);
        workflow.AddChild(GrantApplicationPermissions.Approvals.BulkApplicationApproval);
    }
}
