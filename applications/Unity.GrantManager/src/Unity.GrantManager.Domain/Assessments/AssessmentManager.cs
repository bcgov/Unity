using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Scoresheets.Events;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Permissions;
using Volo.Abp;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.Users;

namespace Unity.GrantManager.Assessments;
public class AssessmentManager : DomainService
{
    private readonly IApplicationManager _applicationManager;
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IApplicationFormRepository _applicationFormRepository;
    private readonly ILocalEventBus _localEventBus;
    private readonly IFeatureChecker _featureChecker;
    private readonly IPermissionChecker _permissionChecker;

    public AssessmentManager(
        IApplicationManager applicationManager,
        IAssessmentRepository assessmentRepository,
        IApplicationFormRepository applicationFormRepository,
        ILocalEventBus localEventBus,
        IFeatureChecker featureChecker,
        IPermissionChecker permissionChecker)
    {
        _applicationManager = applicationManager;
        _assessmentRepository = assessmentRepository;
        _applicationFormRepository = applicationFormRepository;
        _localEventBus = localEventBus;
        _featureChecker = featureChecker;
        _permissionChecker = permissionChecker;
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

        var otherAssessments = await _assessmentRepository.GetListByApplicationId(application.Id);
        bool hasOtherAssessments = otherAssessments != null && otherAssessments.Any(a => !a.IsAiAssessment);
        var startsAssessment = await WouldStartAssessmentAsync(application, hasOtherAssessments);
        if (startsAssessment && !await _permissionChecker.IsGrantedAsync(GrantApplicationPermissions.Assessments.Start))
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.StartAssessmentPermissionRequired);
        }

        var form = await _applicationFormRepository.GetAsync(application.ApplicationFormId);

        var assessment = await _assessmentRepository.InsertAsync(
            new Assessment(
                GuidGenerator.Create(),
                application.Id,
                assessorUser.Id),
            autoSave: true);

        if (startsAssessment)
        {
            await _applicationManager.TriggerAction(application.Id, GrantApplicationAction.Internal_StartAssessment);
        }

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
    /// Whether creating the first human assessment would advance the application's status.
    /// </summary>
    public async Task<bool> RequiresStartAssessmentPermissionAsync(Application application)
    {
        var assessments = await _assessmentRepository.GetListByApplicationId(application.Id);
        var hasHumanAssessments = assessments != null && assessments.Any(assessment => !assessment.IsAiAssessment);
        return await WouldStartAssessmentAsync(application, hasHumanAssessments);
    }

    private async Task<bool> WouldStartAssessmentAsync(Application application, bool hasHumanAssessments)
    {
        return !hasHumanAssessments
            && await _applicationManager.IsActionAllowed(application, GrantApplicationAction.Internal_StartAssessment);
    }

    /// <summary>
    /// Creates and inserts a read-only AI Scoring <see cref="Assessment"/> for an application.
    /// No user is required — the well-known AI Scoring Person is used as the assessor.
    /// Does not affect application state. Idempotent: returns the existing record if one already exists.
    /// </summary>
    /// <param name="application">The application being assessed.</param>
    /// <returns>A new or existing AI <see cref="Assessment"/> for the <see cref="Application"/>.</returns>
    public async Task<Assessment> CreateAiAssessmentAsync(Application application)
    {
        // Idempotency: only one AI assessment per application
        var existing = await _assessmentRepository.FirstOrDefaultAsync(
            x => x.ApplicationId == application.Id && x.IsAiAssessment);
        if (existing != null)
        {
            return existing;
        }

        var form = await _applicationFormRepository.GetAsync(application.ApplicationFormId);

        var newAssessment = new Assessment(
            GuidGenerator.Create(),
            application.Id,
            AIScoringConstants.AiPersonId,
            AssessmentState.COMPLETED)
        {
            IsAiAssessment = true
        };

        var assessment = await _assessmentRepository.InsertAsync(newAssessment, autoSave: true);

        if (form.ScoresheetId != null && await _featureChecker.IsEnabledAsync("Unity.Flex"))
        {
            await _localEventBus.PublishAsync(new CreateScoresheetInstanceEto()
            {
                ScoresheetId = form.ScoresheetId ?? Guid.Empty,
                CorrelationId = assessment.Id,
                CorrelationProvider = "Assessment",
                RelatedCorrelationId = null
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
        return await _assessmentRepository.ExistsAsync(application.Id, assessorUser.Id);
    }
}
