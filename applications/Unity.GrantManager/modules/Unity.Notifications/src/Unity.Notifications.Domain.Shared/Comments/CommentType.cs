using System.Text.Json.Serialization;

namespace Unity.Notifications.Comments;

/// Edit Unity.GrantManager.Comments.CommenType if changing this enum as it is shared between the two projects and used in the API layer.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommentType
{
    ApplicationComment = 0,
    AssessmentComment = 1,
    ApplicantComment = 2,
}
