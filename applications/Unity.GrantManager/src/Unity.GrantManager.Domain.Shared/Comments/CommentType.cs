using System.Text.Json.Serialization;

namespace Unity.GrantManager.Comments;

/// If changing this enum, also update the corresponding Unity.Notifications.Comments.CommentType enum, as it is shared between the two projects and used in the API layer.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommentType
{
    ApplicationComment,
    AssessmentComment,
    ApplicantComment,
}
