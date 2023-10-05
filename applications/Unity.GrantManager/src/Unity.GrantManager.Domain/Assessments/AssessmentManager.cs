using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Users;

namespace Unity.GrantManager.Assessments;
public class AssessmentManager : DomainService
{
    private readonly IAssessmentRepository _assessmentRepository;

    public AssessmentManager(
        IAssessmentRepository assessmentRepository)
    {
        _assessmentRepository = assessmentRepository;
    }

    /// <summary>
    /// Creates and inserts a new <see cref="Assessment"/> for a user.
    /// </summary>
    /// <param name="application">The application being assessed.</param>
    /// <param name="assessorUser">A user assessing the application.</param>
    /// <returns>A new <see cref="Assessment"/> for an <see cref="Application"/>.</returns>
    /// <exception cref="BusinessException">
    /// One or more business domain rules are invalid.
    /// </exception>
    public async Task<Assessment> CreateAsync(
        Application application,
        IUserData assessorUser)
    {
        // Domain Rule: A user can't be assigned to two assessments under the same application
        if (await IsAssignedAsync(application, assessorUser))
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.AssessmentUserAssignmentAlreadyExists);
        }

        return await _assessmentRepository.InsertAsync(
            new Assessment(
                GuidGenerator.Create(),
                application.Id,
                assessorUser.Id),
            autoSave: true);
    }

    /// <summary>
    /// Checks if a user has already been assigned an <see cref="Assessment"/> for an <see cref="Application"/>.
    /// </summary>
    /// <param name="application">The application being assessed.</param>
    /// <param name="assessorUser">A user assessing the application.</param>
    /// <returns>
    /// True if the user is currently assigned to an <see cref="Assessment"/> on an <see cref="Application"/>.
    /// </returns>
    public async Task<bool> IsAssignedAsync(
            Application application,
            IUserData assessorUser)
    {
        return await _assessmentRepository
            .AnyAsync(x =>
                x.ApplicationId == application.Id && x.AssessorId == assessorUser.Id);
    }
}
