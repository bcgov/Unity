using System.Text.Json.Serialization;

namespace Unity.GrantManager.GrantApplications;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GrantApplicationState

{
    // WARNING: DO NOT EDIT ORDER NUMBER WITHOUT UPDATING DB CODE TABLE
    OPEN = 1,
    IN_PROGRESS = 2,
    SUBMITTED = 3,
    ASSIGNED = 4,
    WITHDRAWN = 5,
    CLOSED = 6,
    UNDER_INITIAL_REVIEW = 7,
    INITITAL_REVIEW_COMPLETED = 8,
    UNDER_ASSESSMENT = 9,
    ASSESSMENT_COMPLETED = 10,
    GRANT_APPROVED = 11,
    GRANT_NOT_APPROVED = 12
}


public static class GrantApplicationStateGroups
{
    public static GrantApplicationState[] FinalDecisionStates =  {
        GrantApplicationState.GRANT_APPROVED,
        GrantApplicationState.GRANT_NOT_APPROVED,
        GrantApplicationState.CLOSED,
        GrantApplicationState.WITHDRAWN,
    };
}