using System.Text.Json.Serialization;

namespace Unity.GrantManager.Contacts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContactType
{
    SIGNING_AUTHORITY,
    CONTACT_PERSON,
    OFFICER,
    SUBMITTER,
    CONSULTANT,
    GRANT_WRITER
}
