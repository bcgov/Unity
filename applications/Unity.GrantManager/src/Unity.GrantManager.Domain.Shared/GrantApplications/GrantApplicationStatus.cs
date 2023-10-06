namespace Unity.GrantManager.GrantApplications
{
    public enum GrantApplicationStatus
    {
        // WARNING: DO NOT EDIT ORDER NUMBER WITHOUT UPDATING DB CODE TABLE
        UNDEFINED = 1,
        IN_PROGRESS = 2,
        SUBMITTED = 3,
        ASSIGNED = 4,
        WITHDRAWN = 5,
        CLOSED = 6,
        UNDER_INITIAL_REVIEW = 7,
        INITITAL_REVIEW_COMPLETED = 8,
        UNDER_ADJUDICATION = 9,
        ADJUDICATION_COMPLETED = 10,
        GRANT_APPROVED = 11,
        GRANT_NOT_APPROVED = 12
    }
}
