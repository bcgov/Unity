using Volo.Abp.Reflection;

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

        public static class Assessments
        {
            public const string Default = GroupName + ".Assessments";
            public const string Create = Default + ".Create";
            public const string SendToTeamLead = Default + ".SendToTeamLead";
            public const string SendBack = Default + ".SendBack";
            public const string Confirm = Default + ".Confirm";
        }

        public static class AssessmentResults
        {
            public const string Default = GroupName + ".AssessmentResults";
            public const string Edit = Default + ".Update";
            public const string EditFinalStateFields = Default + ".EditFinalStateFields";
        }

        public static class Payments
        {
            public const string Default = GroupName + ".Payments";
            public const string L1ApproveOrDecline = Default + ".L1ApproveOrDecline";
            public const string L2ApproveOrDecline = Default + ".L2ApproveOrDecline";
            public const string L3ApproveOrDecline = Default + ".L3ApproveOrDecline";
            public const string Decline = Default + ".Decline";
        }

        public static string[] GetAll()
        {
            return ReflectionHelper.GetPublicConstantsRecursively(typeof(GrantApplicationPermissions));
        }
    }
}

//EditApprovedAmount