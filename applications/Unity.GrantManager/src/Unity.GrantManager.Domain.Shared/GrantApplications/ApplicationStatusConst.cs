namespace Unity.GrantManager.GrantApplications;

public static class ApplicationStatusConsts
{
    public const int MaxNameLength = 64;

    public const string IN_PROGRESS = "IN_PROGRESS";
    public const string SUBMITTED = "SUBMITTED";
    public const string ASSIGNED = "ASSIGNED";
    public const string WITHDRAWN = "WITHDRAWN";
    public const string CLOSED = "CLOSED";
    public const string UNDER_INITIAL_REVIEW = "UNDER_INITIAL_REVIEW";
    public const string INITITAL_REVIEW_COMPLETED = "INITITAL_REVIEW_COMPLETED";
    public const string UNDER_ASSESSMENT = "UNDER_ASSESSMENT";
    public const string ASSESSMENT_COMPLETED = "ASSESSMENT_COMPLETED";
    public const string GRANT_APPROVED = "GRANT_APPROVED";
    public const string GRANT_NOT_APPROVED = "GRANT_NOT_APPROVED";
    public const string DEFER = "DEFER";
    public const string ON_HOLD = "ON_HOLD";
}
