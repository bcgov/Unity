using Volo.Abp.Reflection;

namespace Unity.Modules.Shared;

/// <summary>
/// The purpose of this constants class is to set conventional semantics
/// around actions, permissions, zones, and front-end element IDs
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3218:Inner class members should not shadow outer class \"static\" or type members", Justification = "Constants File")]
public static partial class UnitySelector
{
    public static string[] GetAll() => ReflectionHelper.GetPublicConstantsRecursively(typeof(UnitySelector));
    public static string ElementId(this string value) => value.Replace('.', '_');

    public static partial class Applicant
    {
        public const string Default     = "Unity.GrantManager.ApplicationManagement.Applicant";
        public static partial class Authority
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Applicant.Authority";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Applicant.Authority.Create";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Applicant.Authority.Update";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Applicant.Authority.Delete";
        }
        public static partial class Contact
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Applicant.Contact";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Applicant.Contact.Create";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Applicant.Contact.Update";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Applicant.Contact.Delete";
        }
        public static partial class Location
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Applicant.Location";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Applicant.Location.Create";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Applicant.Location.Update";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Applicant.Location.Delete";
        }
        public static partial class Summary
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Applicant.Summary";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Applicant.Summary.Create";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Applicant.Summary.Update";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Applicant.Summary.Delete";
        }
    }
    public static partial class Application
    {
        public const string Default     = "Unity.GrantManager.ApplicationManagement.Application";
        public static partial class Scoresheet
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Application.Scoresheet";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Application.Scoresheet.Create";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Application.Scoresheet.Update";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Application.Scoresheet.Delete";
        }
        public static partial class Summary
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Application.Summary";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Application.Summary.Create";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Application.Summary.Update";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Application.Summary.Delete";
        }
    }
    public static partial class Review
    {
        public const string Default     = "Unity.GrantManager.ApplicationManagement.Review";
        public static partial class Approval
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Review.Approval";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Review.Approval.Create";
            public static partial class Update // Supports Override
            {
                public const string Default  = "Unity.GrantManager.ApplicationManagement.Review.Approval.Update";
            }
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Review.Approval.Delete";
        }

        public static partial class AssessmentResults
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Review.AssessmentResults";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Review.AssessmentResults.Create";
            public static partial class Update // Supports Override
            {
                public const string Default  = "Unity.GrantManager.ApplicationManagement.Review.AssessmentResults.Update";
            }
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Review.AssessmentResults.Delete";
        }

        public static partial class AssessmentReviewList
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Review.AssessmentReviewList";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Review.AssessmentReviewList.Create";
            public static partial class Update // Supports Override
            {
                public const string Default  = "Unity.GrantManager.ApplicationManagement.Review.AssessmentReviewList.Update";
            }
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Review.AssessmentReviewList.Delete";
        }

        public static partial class Worksheet
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Review.Worksheet";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Review.Worksheet.Create";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Review.Worksheet.Update";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Review.Worksheet.Delete";
        }
    }

    public static partial class Attachment
    {
        public const string Default     = "Unity.GrantManager.ApplicationManagement.Attachment";

        public static partial class Review
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Attachment.Review";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Attachment.Review.Create";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Attachment.Review.Update";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Attachment.Review.Delete";
        }
        public static partial class Notification
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Attachment.Notification";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Attachment.Notification.Create";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Attachment.Notification.Update";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Attachment.Notification.Delete";
        }
        public static partial class Submission
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Attachment.Submission";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Attachment.Submission.Create";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Attachment.Submission.Update";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Attachment.Submission.Delete";
        }
    }
    public static partial class Comment
    {
        public const string Default     = "Unity.GrantManager.ApplicationManagement.Comment";
    }
    public static partial class Flex
    {
        public const string Default     = "Unity.GrantManager.ApplicationManagement.Flex";
    }
    public static partial class Funding
    {
        public const string Default     = "Unity.GrantManager.ApplicationManagement.Funding";
        public static partial class Agreement
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Funding.Agreement";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Funding.Agreement.Create";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Funding.Agreement.Update";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Funding.Agreement.Delete";
        }
    }
    public static partial class History
    {
        public const string Default = "Unity.GrantManager.ApplicationManagement.History";
    }
    public static partial class Notification
    {
        public const string Default = "Unity.GrantManager.ApplicationManagement.Notification";
        public const string Create  = "Unity.GrantManager.ApplicationManagement.Notification.Create";
        public const string Update  = "Unity.GrantManager.ApplicationManagement.Notification.Update";
        public const string Delete  = "Unity.GrantManager.ApplicationManagement.Notification.Delete";
        public static partial class Draft
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Notification.Draft";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Notification.Draft.Create";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Notification.Draft.Delete";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Notification.Draft.Update";
        }
    }
    public static partial class Payment
    {
        public const string Default = "Unity.GrantManager.ApplicationManagement.Payment";
        public static partial class Summary
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Payment.Summary";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Payment.Summary.Create";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Payment.Summary.Update";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Payment.Summary.Delete";
        }

        public static partial class Supplier
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Applicant.Supplier";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Applicant.Supplier.Create";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Applicant.Supplier.Update";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Applicant.Supplier.Delete";
        }

        public static partial class PaymentList
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Payment.PaymentList";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Payment.PaymentList.Create";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Payment.PaymentList.Update";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Payment.PaymentList.Delete";
        }
    }
    public static partial class Project
    {
        public const string Default = "Unity.GrantManager.ApplicationManagement.Project";
        public const string UpdatePolicy = "Unity.GrantManager.ApplicationManagement.Project.UpdatePolicy"; // Custom Policy

        public static partial class Location
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Project.Location";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Project.Location.Create";
            public static partial class Update // Supports Override
            {
                public const string Default  = "Unity.GrantManager.ApplicationManagement.Project.Location.Update";
            }
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Project.Location.Delete";
        }
        public static partial class Summary
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Project.Summary";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Project.Summary.Create";
            public static partial class Update // Supports Override
            {
                public const string Default  = "Unity.GrantManager.ApplicationManagement.Project.Summary.Update";
            }
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Project.Summary.Delete";
        }

        public static partial class Worksheet
        {
            public const string Default = "Unity.GrantManager.ApplicationManagement.Project.Worksheet";
            public const string Create  = "Unity.GrantManager.ApplicationManagement.Project.Worksheet.Create";
            public const string Update  = "Unity.GrantManager.ApplicationManagement.Project.Worksheet.Update";
            public const string Delete  = "Unity.GrantManager.ApplicationManagement.Project.Worksheet.Delete";
        }
    }

    public static partial class SettingManagement
    {
        public static class Tags
        {
            public const string Default = "Unity.GrantManager.SettingManagement.Tags";
            public const string Create  = "Unity.GrantManager.SettingManagement.Tags.Create";
            public const string Update  = "Unity.GrantManager.SettingManagement.Tags.Update";
            public const string Delete  = "Unity.GrantManager.SettingManagement.Tags.Delete";
        }
    }
}

