using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Scoresheets.Events;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.Users;

namespace Unity.GrantManager.Assessments;
public class AssessmentManager : DomainService
{
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IApplicationFormRepository _applicationFormRepository;
    private readonly ILocalEventBus _localEventBus;
    private readonly IFeatureChecker _featureChecker;

    public AssessmentManager(
        IAssessmentRepository assessmentRepository,
        IApplicationFormRepository applicationFormRepository,
        ILocalEventBus localEventBus,
        IFeatureChecker featureChecker)
    {
        _assessmentRepository = assessmentRepository;
        _applicationFormRepository = applicationFormRepository;
        _localEventBus = localEventBus;
        _featureChecker = featureChecker;
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

        // Domain Rule: An assessment cannot be created for an application in final state
        if (GrantApplicationStateGroups.FinalDecisionStates.Contains(application.ApplicationStatus.StatusCode))
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.CantCreateAssessmentForFinalStateApplication);
        }

        var form = await _applicationFormRepository.GetAsync(application.ApplicationFormId);

        var otherAssessments = await _assessmentRepository.GetListByApplicationId(application.Id);
        bool hasOtherAssessments = otherAssessments != null && otherAssessments.Count != 0;

        var assessment = await _assessmentRepository.InsertAsync(
            new Assessment(
                GuidGenerator.Create(),
                application.Id,
                assessorUser.Id),
            autoSave: true);
        

        if (form.ScoresheetId != null && await _featureChecker.IsEnabledAsync("Unity.Flex"))
        {
            await _localEventBus.PublishAsync(new CreateScoresheetInstanceEto()
            {
                ScoresheetId = form.ScoresheetId ?? Guid.Empty,
                CorrelationId = assessment.Id,
                CorrelationProvider = "Assessment",
                RelatedCorrelationId = hasOtherAssessments ? otherAssessments![0].Id : null
            });
        }

        return assessment;
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
