using Volo.Abp.Reflection;

namespace Unity.GrantManager.Permissions
{
    public static class GrantApplicationPermissions
    {
        public const string GroupName = "GrantApplicationManagement";
        
        private static class Operation
        {
            public const string Create = ".Create";
            public const string Read = ".Read";
            public const string Update = ".Update";
            public const string Delete = ".Delete";
        }

        public static class Dashboard
        {
            public const string Default                     = GroupName + ".Dashboard";
            public const string ViewDashboard               = Default + ".ViewDashboard";

            public const string ApplicationStatusCount      = Default + ".ApplicationStatusCount";
            public const string EconomicRegionCount         = Default + ".EconomicRegionCount";
            public const string ApplicationTagsCount        = Default + ".ApplicationTagsCount";
            public const string ApplicationAssigneeCount    = Default + ".ApplicationAssigneeCount";
            public const string RequestedAmountPerSubsector = Default + ".RequestedAmountPerSubsector";
            public const string RequestApprovedCount        = Default + ".RequestApprovedCount";
        }

        public static class Applications
        {
            public const string Default = GroupName + ".Applications";
        }

        public static class Applicants
        {
            public const string Default = GroupName + ".Applicants";
            public const string ViewList = Default + ".ViewList";
            public const string Edit = Default + Operation.Update;
            public const string AssignApplicant = Default + ".AssignApplicant";
        }

        public static class AI
        {
            public const string GroupName = "AI";

            public static class Reporting
            {
                public const string Default = GroupName + ".Reporting";
            }

            public static class ApplicationAnalysis
            {
                public const string Default = GroupName + ".ApplicationAnalysis";
            }

            public static class AttachmentSummary
            {
                public const string Default = GroupName + ".AttachmentSummary";
            }
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
            public const string BulkApplicationApproval = Default + ".BulkApplicationApproval";
            public const string DeferAfterApproval = Default + ".DeferAfterApproval";
        }

        public static class Comments
        {
            public const string Default = GroupName + ".Comments";
            public const string Add = Default + ".Add";
        }

        public static class ApplicantInfo
        {
            public const string Default = GroupName + ".ApplicantInfo";
            public const string Create = Default + Operation.Create;
            public const string Read = Default + Operation.Read;
            public const string Update = Default + Operation.Update;
            public const string Delete = Default + Operation.Delete;

            public const string EditOrganization = Default + ".Organization" + Operation.Update;
            public const string EditContact = Default + ".Contact" + Operation.Update;
            public const string EditSigningAuthority = Default + ".SigningAuthority" + Operation.Update;
            public const string EditAddress = Default + ".Address" + Operation.Update;

            public const string AddAdditionalContact = Default + ".AdditionalContact" + Operation.Create;
            public const string UpdateAdditionalContact = Default + ".AdditionalContact" + Operation.Update;
            public const string DeleteAdditionalContact = Default + ".AdditionalContact" + Operation.Delete;
        }

        public static class Payments
        {
            public const string Default = GroupName + ".Payments";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
            
            public static class PaymentRequests
            {
                public const string Default = Payments.Default + ".PaymentRequests";
                public const string CreatePaymentRequest = Default + ".Create";
                public const string EditPaymentRequest = Default + ".Edit";
                public const string DeletePaymentRequest = Default + ".Delete";
            }
        }

        public static string[] GetAll()
        {
            return ReflectionHelper.GetPublicConstantsRecursively(typeof(GrantApplicationPermissions));
        }
    }
}

//EditApprovedAmount