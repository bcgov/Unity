using System.Text.Json.Serialization;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "dataType")]
    [JsonDerivedType(typeof(ApplicantContactInfoDto), "CONTACTINFO")]
    [JsonDerivedType(typeof(ApplicantOrgInfoDto), "ORGINFO")]
    [JsonDerivedType(typeof(ApplicantAddressInfoDto), "ADDRESSINFO")]
    [JsonDerivedType(typeof(ApplicantSubmissionInfoDto), "SUBMISSIONINFO")]
    [JsonDerivedType(typeof(ApplicantPaymentInfoDto), "PAYMENTINFO")]
    public class ApplicantProfileDataDto
    {
        public virtual string DataType { get; } = "";
    }
}
