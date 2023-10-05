using System.Text.Json.Serialization;

namespace Unity.GrantManager.Assessments;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssessmentAction
{
    Create,
    SendTo,
    SendToTeamLead,
    SendBack,
    Confirm
}
