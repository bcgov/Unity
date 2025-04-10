namespace Unity.Modules.Shared;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3218:Inner class members should not shadow outer class \"static\" or type members", Justification = "Constants File")]
public static partial class UnitySelector
{
    public static partial class Review
    {
        public static partial class Approval
        {
            public static partial class Update
            {
                public const string UpdateFinalStateFields  = "Unity.GrantManager.ApplicationManagement.Review.Approval.Update.UpdateFinalStateFields";
            }
        }

        public static partial class AssessmentResults
        {
            public static partial class Update
            {
                public const string UpdateFinalStateFields = "Unity.GrantManager.ApplicationManagement.Review.AssessmentResults.Update.UpdateFinalStateFields";
            }
        }

        public static partial class AssessmentReviewList
        {
            public static partial class Update
            {
                public const string SendBack = "Unity.GrantManager.ApplicationManagement.Review.AssessmentReviewList.Update.SendBack";
                public const string Complete = "Unity.GrantManager.ApplicationManagement.Review.AssessmentReviewList.Update.Complete";
            }
        }
    }
}

