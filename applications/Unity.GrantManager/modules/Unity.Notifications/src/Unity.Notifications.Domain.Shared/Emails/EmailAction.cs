using System.Text.Json.Serialization;

namespace Unity.Notifications.Emails;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmailAction
{
    Retry,
    SendByTemplateId,
    SendFailedSummary,
    SendApproval,
    SendDecline,
    SendCustom,
    SaveDraft,
    SendFsbNotification
}
