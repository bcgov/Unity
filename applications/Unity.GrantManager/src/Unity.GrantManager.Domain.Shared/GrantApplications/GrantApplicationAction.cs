using System.Text.Json.Serialization;

namespace Unity.GrantManager.GrantApplications;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GrantApplicationAction
{
    // Top Level Actions
    Open,
    Close,

    // Opening Actions
    Submit,

    Assign, // Review assignment as a state

    StartReview,
    CompleteReview,

    StartAssessment,
    CompleteAssessment,

    // Closing Actions
    Withdraw,
    Approve,
    Deny
}
