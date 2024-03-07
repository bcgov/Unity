using System.Collections.Generic;
using System.Linq;
using Unity.GrantManager.Permissions;
using Volo.Abp.Users;

namespace Unity.GrantManager.GrantApplications
{
    internal static class ApplicationWorkflowAuthService
    {
        /* magic string cleanup */
        private const string Permission = "Permission";

        private static readonly List<PermissionAllowedStateChange> allowedPermissions = new()
        {
            new PermissionAllowedStateChange(GrantApplicationAction.Approve, new string[] { GrantApplicationPermissions.Approvals.Complete }),
            new PermissionAllowedStateChange(GrantApplicationAction.Deny, new string[] { GrantApplicationPermissions.Approvals.Complete }),
        };

        internal static bool IsAllowedStateChange(GrantApplicationAction targetState, ICurrentUser currentUser)
        {
            var allowedStateChange = allowedPermissions.Find(s => s.TargetAction == targetState);
            if (allowedStateChange == null) return false;

            var userPermissions = currentUser.FindClaims(Permission).Select(s => s.Value);                
            return allowedStateChange.Permissions.ToList().Exists(s => userPermissions.Contains(s));
        }
    }

    internal class PermissionAllowedStateChange
    {
        public PermissionAllowedStateChange(GrantApplicationAction triggerAction, string[] permissions)
        {
            TargetAction = triggerAction;
            Permissions = permissions;
        }

        internal GrantApplicationAction TargetAction { get; set; }
        internal string[] Permissions { get; set; }
    }
}
