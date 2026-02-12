using System.Threading.Tasks;
using Unity.GrantManager.Applicants.ProfileData;

namespace Unity.GrantManager.Applicants.ApplicantProfile
{
    public interface IApplicantProfileDataProvider
    {
        string Key { get; }
        Task<ApplicantProfileDataDto> GetDataAsync(ApplicantProfileInfoRequest request);
    }
}
