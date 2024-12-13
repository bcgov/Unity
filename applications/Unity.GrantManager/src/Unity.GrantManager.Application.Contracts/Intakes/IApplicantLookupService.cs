using System.Threading.Tasks;

namespace Unity.GrantManager.Intakes
{
    public interface IApplicantLookupService
    {
        Task<string> ApplicantLookupByApplicantId(string unityApplicantId);
        Task<string> ApplicantLookupByApplicantName(string unityApplicantName);
    }
}
