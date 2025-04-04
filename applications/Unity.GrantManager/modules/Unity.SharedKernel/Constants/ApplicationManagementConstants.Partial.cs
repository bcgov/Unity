namespace Unity.Modules.Shared.Constants;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3218:Inner class members should not shadow outer class \"static\" or type members", Justification = "Constants File")]
public static partial class ApplicationManagementConstants
{

    public static partial class Applicant
    {
        public static partial class Authority
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Applicant.Authority";
        }
        public static partial class Contact
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Applicant.Contact";
        }
        public static partial class Location
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Applicant.Location";
        }
        public static partial class Summary
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Applicant.Summary";
        }
        public static partial class Supplier
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Applicant.Supplier";
        }
    }
    public static partial class Application
    {
        public static partial class Scoresheet
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Application.Scoresheet";
        }
        public static partial class Summary
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Application.Summary";
        }
    }
    public static partial class Review
    {
        public static partial class Approval
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Review.Approval";
        }
        public static partial class ReviewList
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Review.ReviewList";
        }
        public static partial class Results
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Review.Results";
        }
    }
    public static partial class Attachment
    {
        // TODO: Should this be under Attachment or under Review?
        public static partial class Review
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Attachment.Review";
        }
        public static partial class Notification
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Attachment.Notification";
        }
        public static partial class Submission
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Attachment.Submission";
        }
    }
    public static partial class Comment
    {
        public const string XYZ = "Unity.GrantManger.ApplicationManagement.Comment";
    }
    public static partial class Flex
    {
        public const string XYZ = "Unity.GrantManger.ApplicationManagement.Flex";
    }
    public static partial class Funding
    {
        public static partial class Agreement
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Funding.Agreement";
        }
    }
    public static partial class History
    {
        public const string XYZ = "Unity.GrantManger.ApplicationManagement.History";
    }
    public static partial class Notification
    {
        public const string XYZ = "Unity.GrantManger.ApplicationManagement.Notification";
        public static partial class Draft
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Notification.Draft";
        }
    }
    public static partial class Payment
    {
        public static partial class Summary
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Payment.Summary";
        }
        public static partial class PaymentList
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Payment.PaymentList";
        }
    }
    public static partial class Project
    {
        public static partial class Location
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Project.Location";
        }
        public static partial class Summary
        {
            public const string XYZ = "Unity.GrantManger.ApplicationManagement.Project.Summary";
        }
    }
}

