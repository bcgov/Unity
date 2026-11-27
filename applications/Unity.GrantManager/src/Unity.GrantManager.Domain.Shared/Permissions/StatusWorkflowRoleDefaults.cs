using System;
using System.Collections.Generic;
using Unity.GrantManager.Identity;

namespace Unity.GrantManager.Permissions;

public static class StatusWorkflowRoleDefaults
{
    // Shared by role seeding and permission-cache invalidation.
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> PermissionsByRole =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [UnityRoles.ProgramManager] = Array.AsReadOnly<string>(
            [
                GrantApplicationPermissions.StatusWorkflow.Default,
                GrantApplicationPermissions.Assignments.AssignInitial,
                GrantApplicationPermissions.Reviews.StartInitial,
                GrantApplicationPermissions.Reviews.CompleteInitial,
                GrantApplicationPermissions.Assessments.Start,
                GrantApplicationPermissions.Assessments.Complete
            ]),
            [UnityRoles.TeamLead] = Array.AsReadOnly<string>(
            [
                GrantApplicationPermissions.StatusWorkflow.Default,
                GrantApplicationPermissions.Assignments.AssignInitial,
                GrantApplicationPermissions.Reviews.StartInitial,
                GrantApplicationPermissions.Reviews.CompleteInitial,
                GrantApplicationPermissions.Assessments.Start,
                GrantApplicationPermissions.Assessments.Complete,
                GrantApplicationPermissions.Approvals.DeferAfterApproval,
                GrantApplicationPermissions.Approvals.BulkApplicationApproval
            ]),
            [UnityRoles.Reviewer] = Array.AsReadOnly<string>(
            [
                GrantApplicationPermissions.StatusWorkflow.Default,
                GrantApplicationPermissions.Assignments.AssignInitial,
                GrantApplicationPermissions.Reviews.StartInitial,
                GrantApplicationPermissions.Reviews.CompleteInitial
            ]),
            [UnityRoles.ExternalAssessor] = Array.AsReadOnly<string>(
            [
                GrantApplicationPermissions.StatusWorkflow.Default,
                GrantApplicationPermissions.Assignments.AssignInitial,
                GrantApplicationPermissions.Assessments.Start,
                GrantApplicationPermissions.Assessments.Complete
            ]),
            [UnityRoles.Assessor] = Array.AsReadOnly<string>(
            [
                GrantApplicationPermissions.StatusWorkflow.Default,
                GrantApplicationPermissions.Assignments.AssignInitial,
                GrantApplicationPermissions.Assessments.Start,
                GrantApplicationPermissions.Assessments.Complete
            ]),
            [UnityRoles.Approver] = Array.AsReadOnly<string>(
            [
                GrantApplicationPermissions.StatusWorkflow.Default,
                GrantApplicationPermissions.Assignments.AssignInitial,
                GrantApplicationPermissions.Approvals.Complete,
                GrantApplicationPermissions.Approvals.DeferAfterApproval
            ])
        };

    public static IReadOnlyList<string> ForRole(string roleName) =>
        PermissionsByRole.TryGetValue(roleName, out var permissions) ? permissions : Array.Empty<string>();

    public static IEnumerable<(string RoleName, string PermissionName)> GetAll()
    {
        foreach (var role in PermissionsByRole)
        {
            foreach (var permission in role.Value)
            {
                yield return (role.Key, permission);
            }
        }
    }
}
