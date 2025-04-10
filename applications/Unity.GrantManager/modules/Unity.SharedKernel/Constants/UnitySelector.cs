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


    // public const string Default         = "Unity.GrantManger.ApplicationManagement";

    public enum Operation
    {
        Read,
        Create,
        Update,
        Delete
    }
    public static string On(this string value, Operation action) => $"{value}.{action.ToString()}";

    public static partial class Applicant
    {
        public const string Default     = "Unity.GrantManger.ApplicationManagement.Applicant";
        public static partial class Authority
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Applicant.Authority";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Applicant.Authority.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Applicant.Authority.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Applicant.Authority.Delete";
        }
        public static partial class Contact
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Applicant.Contact";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Applicant.Contact.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Applicant.Contact.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Applicant.Contact.Delete";
        }
        public static partial class Location
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Applicant.Location";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Applicant.Location.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Applicant.Location.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Applicant.Location.Delete";
        }
        public static partial class Summary
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Applicant.Summary";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Applicant.Summary.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Applicant.Summary.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Applicant.Summary.Delete";
        }
        public static partial class Supplier
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Applicant.Supplier";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Applicant.Supplier.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Applicant.Supplier.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Applicant.Supplier.Delete";
        }
    }
    public static partial class Application
    {
        public const string Default     = "Unity.GrantManger.ApplicationManagement.Application";
        public static partial class Scoresheet
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Application.Scoresheet";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Application.Scoresheet.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Application.Scoresheet.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Application.Scoresheet.Delete";
        }
        public static partial class Summary
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Application.Summary";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Application.Summary.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Application.Summary.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Application.Summary.Delete";
        }
    }
    public static partial class Review
    {
        public const string Default     = "Unity.GrantManger.ApplicationManagement.Review";
        public static partial class Approval
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Review.Approval";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Review.Approval.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Review.Approval.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Review.Approval.Delete";
        }

        public static partial class AssessmentResults
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Review.AssessmentResults";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Review.AssessmentResults.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Review.AssessmentResults.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Review.AssessmentResults.Delete";
        }

        public static partial class AssessmentReviewList
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Review.AssessmentReviewList";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Review.AssessmentReviewList.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Review.AssessmentReviewList.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Review.AssessmentReviewList.Delete";
        }

        public static partial class Worksheet
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Review.Worksheet";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Review.Worksheet.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Review.Worksheet.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Review.Worksheet.Delete";
        }
    }

    // TODO: Review this
    public static partial class Attachment
    {
        public const string Default     = "Unity.GrantManger.ApplicationManagement.Attachment";

        // TODO: Should this be under Attachment or under Review?
        public static partial class Review
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Attachment.Review";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Attachment.Review.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Attachment.Review.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Attachment.Review.Delete";
        }
        public static partial class Notification
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Attachment.Notification";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Attachment.Notification.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Attachment.Notification.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Attachment.Notification.Delete";
        }
        public static partial class Submission
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Attachment.Submission";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Attachment.Submission.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Attachment.Submission.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Attachment.Submission.Delete";
        }
    }
    public static partial class Comment
    {
        public const string Default     = "Unity.GrantManger.ApplicationManagement.Comment";
    }
    public static partial class Flex
    {
        public const string Default     = "Unity.GrantManger.ApplicationManagement.Flex";
    }
    public static partial class Funding
    {
        public const string Default     = "Unity.GrantManger.ApplicationManagement.Funding";
        public static partial class Agreement
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Funding.Agreement";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Funding.Agreement.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Funding.Agreement.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Funding.Agreement.Delete";
        }
    }
    public static partial class History
    {
        public const string Default = "Unity.GrantManger.ApplicationManagement.History";
    }
    public static partial class Notification
    {
        public const string Default = "Unity.GrantManger.ApplicationManagement.Notification";
        public const string Create  = "Unity.GrantManger.ApplicationManagement.Notification.Create";
        public const string Update  = "Unity.GrantManger.ApplicationManagement.Notification.Update";
        public const string Delete  = "Unity.GrantManger.ApplicationManagement.Notification.Delete";
        public static partial class Draft
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Notification.Draft";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Notification.Draft.Create";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Notification.Draft.Delete";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Notification.Draft.Update";
        }
    }
    public static partial class Payment
    {
        public const string Default = "Unity.GrantManger.ApplicationManagement.Payment";
        public static partial class Summary
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Payment.Summary";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Payment.Summary.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Payment.Summary.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Payment.Summary.Delete";
        }
        public static partial class PaymentList
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Payment.PaymentList";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Payment.PaymentList.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Payment.PaymentList.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Payment.PaymentList.Delete";
        }
    }
    public static partial class Project
    {
        public const string Default = "Unity.GrantManger.ApplicationManagement.Project";
        public static partial class Location
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Project.Location";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Project.Location.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Project.Location.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Project.Location.Delete";
        }
        public static partial class Summary
        {
            public const string Default = "Unity.GrantManger.ApplicationManagement.Project.Summary";
            public const string Create  = "Unity.GrantManger.ApplicationManagement.Project.Summary.Create";
            public const string Update  = "Unity.GrantManger.ApplicationManagement.Project.Summary.Update";
            public const string Delete  = "Unity.GrantManger.ApplicationManagement.Project.Summary.Delete";
        }
    }
}

