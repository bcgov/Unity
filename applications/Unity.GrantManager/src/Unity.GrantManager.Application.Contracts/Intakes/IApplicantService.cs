using System.Threading.Tasks;

namespace Unity.GrantManager.Intakes
{
    public interface IApplicantService
    {
        Task<string> ApplicantLookupByApplicantId(string? unityApplicantId);
    }
}
