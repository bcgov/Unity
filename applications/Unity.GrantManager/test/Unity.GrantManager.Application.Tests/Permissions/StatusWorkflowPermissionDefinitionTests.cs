using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Localization;
using Unity.Modules.Shared;
using Volo.Abp.Authorization.Permissions;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.Permissions;

public class StatusWorkflowPermissionDefinitionTests(ITestOutputHelper output) : GrantManagerApplicationTestBase(output)
{
    [Fact]
    public async Task Should_Group_The_Eight_Actions_Under_Status_Workflow_In_Order()
    {
        var manager = GetRequiredService<IPermissionDefinitionManager>();
        var workflow = await manager.GetAsync(GrantApplicationPermissions.StatusWorkflow.Default);

        workflow.Parent.ShouldBeNull();
        workflow.Children.Select(child => child.Name).ShouldBe(new[]
        {
            GrantApplicationPermissions.Assignments.AssignInitial,
            GrantApplicationPermissions.Reviews.StartInitial,
            GrantApplicationPermissions.Reviews.CompleteInitial,
            GrantApplicationPermissions.Assessments.Start,
            GrantApplicationPermissions.Assessments.Complete,
            GrantApplicationPermissions.Approvals.Complete,
            GrantApplicationPermissions.Approvals.DeferAfterApproval,
            GrantApplicationPermissions.Approvals.BulkApplicationApproval
        });
        workflow.Children.ShouldAllBe(child => child.Parent == workflow);

        var permissions = await manager.GetPermissionsAsync();
        permissions.Count(permission => permission.Name == GrantApplicationPermissions.Assessments.Start).ShouldBe(1);
        permissions.Count(permission => permission.Name == GrantApplicationPermissions.Assessments.Complete).ShouldBe(1);
        permissions.ShouldContain(permission => permission.Name == UnitySelector.Review.AssessmentReviewList.Update.Complete);

        var retiredGroups = new[] { GrantApplicationPermissions.Assignments.Default,
            GrantApplicationPermissions.Reviews.Default, GrantApplicationPermissions.Approvals.Default };
        permissions.ShouldNotContain(permission => retiredGroups.Contains(permission.Name));
        var options = GetRequiredService<IOptions<AbpPermissionOptions>>().Value;
        retiredGroups.ShouldAllBe(name => options.DeletedPermissions.Contains(name));
    }

    [Fact]
    public void Should_Display_The_Requested_Permission_Labels()
    {
        var localizer = GetRequiredService<IStringLocalizer<GrantManagerResource>>();
        localizer["Permission:GrantApplicationManagement"].Value.ShouldBe("Grant Application Management");
        localizer["Permission:GrantApplicationManagement.StatusWorkflow.Default"].Value.ShouldBe("Status Workflow");
        localizer["Permission:GrantApplicationManagement.Approvals.Complete"].Value.ShouldBe("Complete Approval/Decline");
    }
}
