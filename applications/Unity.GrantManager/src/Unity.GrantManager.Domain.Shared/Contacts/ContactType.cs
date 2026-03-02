using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Unity.GrantManager.Contacts;

public static class ContactTypes
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApplicantContactTypes
    {
        [Display(Name = "Signing Authority")]
        SIGNING_AUTHORITY,

        [Display(Name = "Contact Person")]
        CONTACT_PERSON,

        [Display(Name = "Officer")]
        OFFICER,

        [Display(Name = "Submitter")]
        SUBMITTER,

        [Display(Name = "Consultant")]
        CONSULTANT,

        [Display(Name = "Grant Writer")]
        GRANT_WRITER
    }
}

