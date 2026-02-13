using System.Threading.Tasks;
using Unity.GrantManager.Applicants.ProfileData;

namespace Unity.GrantManager.Applicants.ApplicantProfile
{
    /// <summary>
    /// Defines a contract for components that can provide applicant profile data
    /// based on an <see cref="ApplicantProfileInfoRequest"/>.
    /// </summary>
    public interface IApplicantProfileDataProvider
    {
        /// <summary>
        /// Gets the unique key that identifies this applicant profile data provider.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Asynchronously retrieves applicant profile data for the specified request.
        /// </summary>
        /// <param name="request">The request containing the information needed to resolve the applicant profile.</param>
        /// <returns>
        /// A task that, when completed successfully, returns an <see cref="ApplicantProfileDataDto"/>
        /// containing the resolved applicant profile data.
        /// </returns>
        Task<ApplicantProfileDataDto> GetDataAsync(ApplicantProfileInfoRequest request);
    }
}
