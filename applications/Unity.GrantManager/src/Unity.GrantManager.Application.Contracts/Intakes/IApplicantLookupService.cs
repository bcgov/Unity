using System.Threading.Tasks;

namespace Unity.GrantManager.Intakes
{
    public interface IApplicantLookupService
    {
        Task<string> ApplicantLookupByApplicantId(string unityApplicantId);
        Task<string> ApplicantLookupByBceidBusinesName(string bceidBusinessName, bool createIfNotExists = false);
    }
}
