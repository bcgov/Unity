using System.Text.Json;
using System.Text.Json.Serialization;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    /// <summary>
    /// Carries the form.io form definition (<see cref="Schema"/>) and the submitted answers
    /// (<see cref="Data"/>, itself a full form.io submission object, e.g. <c>{ "data": {...} }</c>)
    /// for a single submission, so the Applicant Portal can render a client-side PDF.
    /// </summary>
    public class ApplicantSubmissionFormDataDto : ApplicantProfileDataDto
    {
        [JsonIgnore]
        public override string DataType => "SUBMISSIONFORMDATA";

        public JsonElement Schema { get; set; }
        public JsonElement Data { get; set; }
    }
}
