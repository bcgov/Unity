using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Identity;
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

    public async Task<Assessment> CreateAsync(
        Application application,
        IUserData assignedUser)
    {
        // Domain Rule: A user can't be assigned to two assessments under the same application
        if (await IsAssignedAsync(application, assignedUser))
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.AssessmentUserAssignmentAlreadyExists);
        }

        return await _assessmentRepository.InsertAsync(
            new Assessment(
                GuidGenerator.Create(),
                application.Id,
                assignedUser.Id),
            autoSave: true);
    }

    public async Task<bool> IsAssignedAsync(
            Application application,
            IUserData assignedUser)
    {
        return await _assessmentRepository
            .AnyAsync(x =>
                x.ApplicationId == application.Id && x.AssignedUserId == assignedUser.Id);
    }
}
