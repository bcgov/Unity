namespace Unity.Modules.Shared.Permissions
{
    public static class IdentityConsts
    {
        public const string ITAdminPolicyName = "ITAdministrator";
        public const string ITAdminRoleName = "ITAdministrator";
        public const string ITAdminPermissionName = "ITAdministrator";

        public const string ITOperationsPolicyName = "ITOperations";
        public const string ITOperationsRoleName = "ITOperations";
        public const string ITOperationsPermissionName = "ITOperations";

        // ITAdministrator is a superset of ITOperations - anywhere ITOperations alone is
        // required, ITAdministrator should also be allowed. Use this policy (RequireRole is an
        // OR check across the given roles) instead of ITOperationsPolicyName when that's needed.
        public const string ITAdminOrITOperationsPolicyName = "ITAdminOrITOperations";
    }
}
