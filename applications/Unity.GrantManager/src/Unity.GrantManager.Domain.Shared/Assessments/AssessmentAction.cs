using System.Text.Json.Serialization;

namespace Unity.GrantManager.Assessments;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssessmentAction : int
{
    Create,
    SendTo,
    SendToTeamLead,
    SendBack,
    Confirm
}
