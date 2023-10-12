using System.Text.Json.Serialization;

namespace Unity.GrantManager.GrantApplications;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GrantApplicationAction
{
    // Opening Actions
    Open,
    Submit,

    Assign,

    StartAssessment,
    CompleteAssessment,

    StartReview,
    CompleteReview,

    // Closing Actions
    Approve,
    Deny,

    Close,
    Withdraw
}
