namespace Unity.Modules.Shared;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3218:Inner class members should not shadow outer class \"static\" or type members", Justification = "Constants File")]
public static partial class UnitySelector
{
    public static partial class ApplicantManagement
    {
        public const string Default = "Unity.GrantManager.ApplicantManagement";
        public static partial class ApplicantInfo
        {
            public const string Default = "Unity.GrantManager.ApplicantManagement.ApplicantInfo";
            public const string Create  = "Unity.GrantManager.ApplicantManagement.ApplicantInfo.Create";
            public const string Update  = "Unity.GrantManager.ApplicantManagement.ApplicantInfo.Update";
            public const string Delete  = "Unity.GrantManager.ApplicantManagement.ApplicantInfo.Delete";
            public const string EditApplicantInfo    = "Unity.GrantManager.ApplicantManagement.ApplicantInfo.EditApplicantInfo";
            public const string EditRedStop          = "Unity.GrantManager.ApplicantManagement.ApplicantInfo.EditRedStop";
            public const string EditOrganizationInfo = "Unity.GrantManager.ApplicantManagement.ApplicantInfo.EditOrganizationInfo";
        }

        public static partial class Contacts
        {
            public const string Default = "Unity.GrantManager.ApplicantManagement.Contacts";
            public const string Create  = "Unity.GrantManager.ApplicantManagement.Contact.Create";
            public const string Update  = "Unity.GrantManager.ApplicantManagement.Contact.Update";
            public const string Delete  = "Unity.GrantManager.ApplicantManagement.Contact.Delete";
            public const string EditContacts = "Unity.GrantManager.ApplicantManagement.Contacts.EditContacts";
        }

        public static partial class Addresses
        {
            public const string Default = "Unity.GrantManager.ApplicantManagement.Addresses";
            public const string Create  = "Unity.GrantManager.ApplicantManagement.Addresses.Create";
            public const string Update  = "Unity.GrantManager.ApplicantManagement.Addresses.Update";
            public const string Delete  = "Unity.GrantManager.ApplicantManagement.Addresses.Delete";
            public const string EditAddresses = "Unity.GrantManager.ApplicantManagement.Addresses.EditAddresses";
        }

        public static partial class Submissions
        {
            public const string Default         = "Unity.GrantManager.ApplicantManagement.Submissions";
            public const string AssignApplicant = "Unity.GrantManager.ApplicantManagement.Submissions.AssignApplicant";
        }

        public static partial class Payments
        {
            public const string Default          = "Unity.GrantManager.ApplicantManagement.Payments";
            public const string EditSupplierInfo = "Unity.GrantManager.ApplicantManagement.Payments.EditSupplierInfo";
        }

        public static partial class History
        {
            public const string Default            = "Unity.GrantManager.ApplicantManagement.History";
            public const string EditFundingHistory = "Unity.GrantManager.ApplicantManagement.History.EditFundingHistory";
            public const string EditAuditHistory   = "Unity.GrantManager.ApplicantManagement.History.EditAuditHistory";
            public const string EditIssueHistory   = "Unity.GrantManager.ApplicantManagement.History.EditIssueHistory";

            public static partial class FundingHistory
            {
                public const string Update = "Unity.GrantManager.ApplicantManagement.History.FundingHistory.Update";
            }

            public static partial class AuditHistory
            {
                public const string Update = "Unity.GrantManager.ApplicantManagement.History.AuditHistory.Update";
            }

            public static partial class IssueHistory
            {
                public const string Update = "Unity.GrantManager.ApplicantManagement.History.IssueHistory.Update";
            }
        }

        public static partial class Comments
        {
            public const string Default             = "Unity.GrantManager.ApplicantManagement.Comments";
            public const string AddApplicantComment = "Unity.GrantManager.ApplicantManagement.Comments.AddApplicantComment";
        }

        public static partial class Attachments
        {
            public const string Default   = "Unity.GrantManager.ApplicantManagement.Attachments";
            public const string Upload    = "Unity.GrantManager.ApplicantManagement.Attachments.Upload";
            public const string EditLabel = "Unity.GrantManager.ApplicantManagement.Attachments.EditLabel";
        }

        public static partial class Merge
        {
            public const string Default = "Unity.GrantManager.ApplicantManagement.Merge";
        }
    }
}
