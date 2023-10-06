using System.Text.Json.Serialization;

namespace Unity.GrantManager.Assessments;

// NOTE: Max Length: Do not exceed status values that are longer than 50 characters
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssessmentState
{
    IN_PROGRESS,
    IN_REVIEW,
    COMPLETED
}
