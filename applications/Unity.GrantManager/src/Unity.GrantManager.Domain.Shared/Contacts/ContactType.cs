using System.Text.Json.Serialization;

namespace Unity.GrantManager.Contacts;

public static class ContactTypes
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApplicantContactTypes
    {
        SIGNING_AUTHORITY,
        CONTACT_PERSON,
        OFFICER,
        SUBMITTER,
        CONSULTANT,
        GRANT_WRITER
    }
}
