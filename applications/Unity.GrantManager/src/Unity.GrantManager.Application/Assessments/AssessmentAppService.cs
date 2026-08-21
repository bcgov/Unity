using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Permissions;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Workflow;
using Unity.Modules.Shared;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;
using Volo.Abp.Identity.Integration;
using Volo.Abp.Users;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Assessments
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(AssessmentAppService), typeof(IAssessmentAppService))]
    public class AssessmentAppService : ApplicationService, IAssessmentAppService
    {
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly AssessmentManager _assessmentManager;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IIdentityUserIntegrationService _userLookupProvider;
        private readonly IFeatureChecker _featureChecker;
        private readonly IAssessmentScoresheetService _assessmentScoresheetService;

        public AssessmentAppService(
            IAssessmentRepository assessmentRepository,
            AssessmentManager assessmentManager,
            IApplicationRepository applicationRepository,
            IIdentityUserIntegrationService userLookupProvider,
            IFeatureChecker featureChecker,
            IAssessmentScoresheetService assessmentScoresheetService)
        {
            _assessmentRepository = assessmentRepository;
            _assessmentManager = assessmentManager;
            _applicationRepository = applicationRepository;
            _userLookupProvider = userLookupProvider;
            _featureChecker = featureChecker;
            _assessmentScoresheetService = assessmentScoresheetService;
        }

        public async Task<AssessmentDto> CreateAsync(CreateAssessmentDto dto)
        {
            Application application = await _applicationRepository.GetAsync(dto.ApplicationId);
            IUserData currentUser = await _userLookupProvider.FindByIdAsync(CurrentUser.GetId());

            var result = await _assessmentManager.CreateAsync(application, currentUser);

            // Fire the event
            return ObjectMapper.Map<Assessment, AssessmentDto>(result);
        }

        public async Task<IList<AssessmentDto>> GetListAsync(Guid applicationId)
        {
            IQueryable<Assessment> queryableAssessments = await _assessmentRepository.GetQueryableAsync();
            var assessments = queryableAssessments.Where(c => c.ApplicationId.Equals(applicationId)).ToList();
            return ObjectMapper.Map<List<Assessment>, List<AssessmentDto>>(
                assessments.OrderByDescending(s => s.IsAiAssessment).ThenByDescending(s => s.CreationTime).ToList());
        }

        public async Task<AssessmentDisplayListDto> GetDisplayList(Guid applicationId)
        {
            var assessments = await _assessmentRepository.GetListWithAssessorsAsync(applicationId);
            var assessmentList = ObjectMapper.Map<List<AssessmentWithAssessorQueryResultItem>, List<AssessmentListItemDto>>(assessments);

            // If AI Scoring feature is disabled or user lacks permission, filter out AI assessments
            var aiScoringEnabled = await _featureChecker.IsEnabledAsync("Unity.AI.Scoring");
            var canViewAI = await AuthorizationService.IsGrantedAsync(AIPermissions.Analysis.ViewScoringResult);
            assessmentList = assessmentList
                .Where(a => !a.IsAiAssessment || (aiScoringEnabled && canViewAI))
                .OrderByDescending(a => a.IsAiAssessment)
                .ThenByDescending(a => a.StartDate)
                .ToList();

            bool isApplicationUsingDefaultScoresheet = true;
            foreach (var assessment in assessmentList)
            {
                var subtotalDto = await _assessmentScoresheetService.GetSubTotalAsync(assessment);
                assessment.SubTotal = subtotalDto.SubTotal;
                if (!subtotalDto.IsUsingDefaultScoresheet)
                {
                    isApplicationUsingDefaultScoresheet = false;
                }
            }

            if (assessmentList.Count == 0)
            {
                var application = await _applicationRepository.GetAsync(applicationId);
                isApplicationUsingDefaultScoresheet = await _assessmentScoresheetService.IsScoresheetNotLinkedToFormAsync(application.ApplicationFormId);
            }

            return new AssessmentDisplayListDto { Data = assessmentList, IsApplicationUsingDefaultScoresheet = isApplicationUsingDefaultScoresheet };
        }

        /// <summary>
        /// If exists, returns the current user's Assessment for an Application.
        /// </summary>
        /// <param name="applicationId">The application under assessment.</param>
        /// <returns>
        /// Returns the assessmentId for the current user assigned to the application.
        /// Returns null if the current user has no assessment for the application.
        /// </returns>
        public async Task<Guid?> GetCurrentUserAssessmentId(Guid applicationId)
        {
            var assessment = await _assessmentRepository
                .FindAsync(x => x.ApplicationId == applicationId && x.AssessorId == CurrentUser.GetId());
            return assessment?.Id;
        }

        public async Task UpdateAssessmentRecommendation(UpdateAssessmentRecommendationDto dto)
        {
            var assessment = await _assessmentRepository.GetAsync(dto.AssessmentId);
            if (assessment != null)
            {
                if (assessment.IsAiAssessment)
                {
                    throw new BusinessException(GrantManagerDomainErrorCodes.CannotModifyAiAssessment);
                }
                assessment.ApprovalRecommended = dto.ApprovalRecommended;
                await _assessmentRepository.UpdateAsync(assessment);
            }
        }    

        #region ASSESSMENT WORKFLOW
        /// <summary>
        /// Get all actions configured for the Assessment workflow.
        /// </summary>
        public List<AssessmentAction> GetAllActions()
        {
            var blankAssessment = new Assessment();
            return blankAssessment.Workflow.GetAllActions().Distinct().ToList();
        }

        /// <summary>
        /// Get all permitted actions for an Assessment given its state.
        /// </summary>
        public async Task<List<AssessmentAction>> GetPermittedActions(Guid assessmentId)
        {
            var assessment = await _assessmentRepository.GetAsync(assessmentId);
            var workflowActions = await assessment.Workflow.GetPermittedActionsAsync();

            List<AssessmentAction> permittedActions = new();
            foreach (var triggerAction in workflowActions)
            {
                var currentRequirement = GetActionAuthorizationRequirement(triggerAction);
                if (await AuthorizationService.IsGrantedAsync(assessment, currentRequirement))
                {
                    permittedActions.Add(triggerAction);
                }
            }

            return permittedActions;
        }

        /// <summary>
        /// Generate a Mermaid graph from the Asssessment workflow.
        /// </summary>
        public static string? GetWorkflowDiagram()
        {
            var assessment = new Assessment();
            return assessment.Workflow.GetWorkflowDiagram();
        }

        /// <summary>
        /// Transitions the Assessment's workflow state machine given an action.
        /// </summary>
        /// <param name="assessmentId">The Assessment</param>
        /// <param name="triggerAction">The action to be invoked on an Assessment</param>
        public async Task<AssessmentDto> ExecuteAssessmentAction(Guid assessmentId, AssessmentAction triggerAction)
        {
            var assessment = await _assessmentRepository.GetAsync(assessmentId);

            if (assessment.IsAiAssessment)
            {
                throw new BusinessException(GrantManagerDomainErrorCodes.CannotModifyAiAssessment);
            }

            await AuthorizationService.CheckAsync(assessment, GetActionAuthorizationRequirement(triggerAction));

            await _assessmentScoresheetService.ValidateAnswersOnCompleteAsync(assessmentId, triggerAction);

            await assessment.Workflow.ExecuteActionAsync(triggerAction);

            return ObjectMapper.Map<Assessment, AssessmentDto>(await _assessmentRepository.UpdateAsync(assessment, autoSave: true));
        }

        private static OperationAuthorizationRequirement GetActionAuthorizationRequirement(AssessmentAction triggerAction)
        {
            if (triggerAction == AssessmentAction.SendBack || triggerAction == AssessmentAction.Complete)
            {
                // Actions that require parent Update permissions
                return new OperationAuthorizationRequirement { Name = $"{UnitySelector.Review.AssessmentReviewList.Update.Default}.{triggerAction}" };

            } else
            {
                // Actions for generic Create, Update, Delete permissions
                return new OperationAuthorizationRequirement { Name = $"{UnitySelector.Review.AssessmentReviewList.Default}.{triggerAction}" };
            }
        }
        #endregion ASSESSMENT WORKFLOW

        public async Task UpdateAssessmentScore(AssessmentScoresDto dto)
        {
            /*
             * Important! Something to do in the future:
             *    -- need to revisit scoring again post-MVP as right now it is only offline scoring
             *    -- need to leverage state machine and domain layer during the revisit
             */
            try
            {
                var assessment = await _assessmentRepository.GetAsync(dto.AssessmentId);
                if (assessment != null)
                {
                    if (assessment.IsAiAssessment)
                    {
                        throw new BusinessException(GrantManagerDomainErrorCodes.CannotModifyAiAssessment);
                    }
                    if (CurrentUser.GetId() != assessment.AssessorId)
                    {
                        throw new AbpValidationException("Error: You do not own this assessment record.");
                    }
                    if (assessment.Status.Equals(AssessmentState.COMPLETED))
                    {
                        throw new AbpValidationException("Error: This assessment is already completed.");
                    }
                    assessment.FinancialAnalysis = dto.FinancialAnalysis;
                    assessment.EconomicImpact = dto.EconomicImpact;
                    assessment.InclusiveGrowth = dto.InclusiveGrowth;
                    assessment.CleanGrowth = dto.CleanGrowth;
                    await _assessmentRepository.UpdateAsync(assessment);
                }
                else
                {
                    throw new AbpValidationException("AssessmentId Not Found: " + dto.AssessmentId + ".");
                }
            }
            catch (Exception ex)
            {
                throw new AbpValidationException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Creates a new human assessment by cloning an existing AI assessment.
        /// Copies the AI scoresheet answers (from AI.ApplicationScoresheetAnswers) as real
        /// Answer records on the new assessment's scoresheet instance, and carries over
        /// ApprovalRecommended as a starting point for the reviewer.
        /// </summary>
        /// <param name="aiAssessmentId">The ID of the source AI assessment to clone from.</param>
        /// <returns>The newly created human <see cref="AssessmentDto"/>.</returns>
        /// <exception cref="BusinessException">
        /// Thrown when the specified assessment is not an AI assessment.
        /// </exception>
        [Authorize(AIPermissions.Analysis.ViewScoringResult)]
        public async Task<AssessmentDto> CloneFromAiAsync(Guid aiAssessmentId)
        {
            if (!await _featureChecker.IsEnabledAsync("Unity.AI.Scoring"))
            {
                throw new UserFriendlyException("AI scoring is not enabled.");
            }

            var aiAssessment = await _assessmentRepository.GetAsync(aiAssessmentId);
            if (!aiAssessment.IsAiAssessment)
            {
                throw new BusinessException(GrantManagerDomainErrorCodes.CannotCloneNonAiAssessment);
            }

            var application = await _applicationRepository.GetAsync(aiAssessment.ApplicationId);
            var currentUser = await _userLookupProvider.FindByIdAsync(CurrentUser.GetId());
            var newAssessment = await _assessmentManager.CreateAsync(application, currentUser);

            newAssessment.ApprovalRecommended = aiAssessment.ApprovalRecommended;
            await _assessmentRepository.UpdateAsync(newAssessment);

            await _assessmentScoresheetService.CopyAiAnswersIfEnabledAsync(aiAssessment.ApplicationId, newAssessment.Id);

            return ObjectMapper.Map<Assessment, AssessmentDto>(newAssessment);
        }

        public async Task SaveScoresheetSectionAnswers(AssessmentScoreSectionDto dto)
        {
            var assessment = await _assessmentRepository.GetAsync(dto.AssessmentId);
            try
            {
                if (assessment != null)
                {
                    if (assessment.IsAiAssessment)
                    {
                        throw new BusinessException(GrantManagerDomainErrorCodes.CannotModifyAiAssessment);
                    }
                    if (CurrentUser.GetId() != assessment.AssessorId)
                    {
                        throw new AbpValidationException("Error: You do not own this assessment record.");
                    }
                    if (assessment.Status.Equals(AssessmentState.COMPLETED))
                    {
                        throw new AbpValidationException("Error: This assessment is already completed.");
                    }

                    await _assessmentScoresheetService.PersistSectionAnswersIfEnabledAsync(dto.AssessmentId, dto.AssessmentAnswers);
                }
                else
                {
                    throw new AbpValidationException("AssessmentId Not Found: " + dto.AssessmentId + ".");
                }

            }
            catch (Exception ex)
            {
                throw new AbpValidationException(ex.Message, ex);
            }
        }

    }
}

