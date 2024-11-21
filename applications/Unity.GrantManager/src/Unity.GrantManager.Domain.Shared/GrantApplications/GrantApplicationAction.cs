using System.Text.Json.Serialization;

namespace Unity.GrantManager.GrantApplications;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GrantApplicationAction
{
    // Opening Actions
    Open,
    Submit,

    Internal_Assign,
    Internal_Unasign,

    StartReview,
    CompleteReview,

    Internal_StartAssessment,
    StartAssessment,
    CompleteAssessment,

    // Closing Actions
    Approve,
    Deny,

    Close,
    Withdraw,

    Defer,
    OnHold
}
