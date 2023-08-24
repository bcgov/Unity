namespace Unity.GrantManager.Permissions
{
    public static class GrantApplicationPermissions
    {
        public const string GroupName = "GrantApplicationManagement";

        public static class Applications
        {
            public const string Default = GroupName + ".Applications";
        }

        public static class Applicants
        {
            public const string Default = GroupName + ".Applicants";
            public const string Edit = Default + ".Update";
        }

        public static class Assignments
        {
            public const string Default = GroupName + ".Assignments";
            public const string AssignInitial = Default + ".AssignInitial";
        }

        public static class Reviews
        {
            public const string Default = GroupName + ".Reviews";
            public const string StartInitial = Default + ".StartInitial";
            public const string CompleteInitial = Default + ".CompleteInitial";
        }

        public static class Adjudications
        {
            public const string Default = GroupName + ".Adjudications";
            public const string Start = Default + ".Start";
            public const string Complete = Default + ".Complete";
        }

        public static class Approvals
        {
            public const string Default = GroupName + ".Approvals";
            public const string Complete = Default + ".Complete";
        }

        public static class Comments
        {
            public const string Default = GroupName + ".Comments";
            public const string Add = Default + ".Add";
        }
    }
}
