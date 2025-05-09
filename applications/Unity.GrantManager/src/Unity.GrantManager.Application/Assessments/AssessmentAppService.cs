using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex;
using Unity.Flex.Scoresheets;
using Unity.Flex.Scoresheets.Enums;
using Unity.Flex.Scoresheets.Events;
using Unity.Flex.Worksheets.Definitions;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Comments;
using Unity.GrantManager.Exceptions;
using Unity.GrantManager.Workflow;
using Unity.Modules.Shared;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
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
        private readonly ICommentsManager _commentsManager;
        private readonly IScoresheetInstanceAppService _scoresheetInstanceAppService;
        private readonly IScoresheetAppService _scoresheetAppService;
        private readonly ILocalEventBus _localEventBus;
        private readonly IFeatureChecker _featureChecker;
        private readonly IRepository<ApplicationForm, Guid> _applicationFormRepository;
        private const string UnityFlex = "Unity.Flex";

        public AssessmentAppService(
            IAssessmentRepository assessmentRepository,
            AssessmentManager assessmentManager,
            IApplicationRepository applicationRepository,
            IIdentityUserIntegrationService userLookupProvider,
            ICommentsManager commentsManager,
            IScoresheetInstanceAppService scoresheetInstanceAppService,
            IFeatureChecker featureChecker,
            ILocalEventBus localEventBus,
            IScoresheetAppService scoresheetAppService,
            IRepository<ApplicationForm, Guid> applicationFormRepository)
        {
            _assessmentRepository = assessmentRepository;
            _assessmentManager = assessmentManager;
            _applicationRepository = applicationRepository;
            _userLookupProvider = userLookupProvider;
            _commentsManager = commentsManager;
            _scoresheetInstanceAppService = scoresheetInstanceAppService;
            _scoresheetAppService = scoresheetAppService;
            _featureChecker = featureChecker;
            _localEventBus = localEventBus;
            _applicationFormRepository = applicationFormRepository;
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
            IQueryable<Assessment> queryableAssessments = _assessmentRepository.GetQueryableAsync().Result;
            var assessments = queryableAssessments.Where(c => c.ApplicationId.Equals(applicationId)).ToList();
            return await Task.FromResult<IList<AssessmentDto>>(ObjectMapper.Map<List<Assessment>, List<AssessmentDto>>(assessments.OrderByDescending(s => s.CreationTime).ToList()));
        }

        public async Task<AssessmentDisplayListDto> GetDisplayList(Guid applicationId)
        {
            var assessments = await _assessmentRepository.GetListWithAssessorsAsync(applicationId);
            var assessmentList = ObjectMapper.Map<List<AssessmentWithAssessorQueryResultItem>, List<AssessmentListItemDto>>(assessments);
            bool isApplicationUsingDefaultScoresheet = true;
            foreach (var assessment in assessmentList)
            {
                var subtotalDto = await GetSubTotal(assessment);
                assessment.SubTotal = subtotalDto.SubTotal;
                if (!subtotalDto.IsUsingDefaultScoresheet)
                {
                    isApplicationUsingDefaultScoresheet = false;
                }
            }

            if (assessmentList.Count == 0)
            {
                isApplicationUsingDefaultScoresheet = await IsScoresheetNotLinkedToForm(applicationId);
            }

            return new AssessmentDisplayListDto { Data = assessmentList, IsApplicationUsingDefaultScoresheet = isApplicationUsingDefaultScoresheet };
        }

        private async Task<bool> IsScoresheetNotLinkedToForm(Guid applicationId)
        {
            var application = await _applicationRepository.GetAsync(applicationId);
            var applicationForm = await _applicationFormRepository.GetAsync(application.ApplicationFormId);
            if (applicationForm.ScoresheetId == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<SubTotalDto> GetSubTotal(AssessmentListItemDto assessment)
        {
            if (await _featureChecker.IsEnabledAsync(UnityFlex))
            {
                var instance = await _scoresheetInstanceAppService.GetByCorrelationAsync(assessment.Id);

                if (instance == null)
                {

                    double subTotal = (assessment.FinancialAnalysis ?? 0) + (assessment.EconomicImpact ?? 0) + (assessment.InclusiveGrowth ?? 0) + (assessment.CleanGrowth ?? 0);
                    return new SubTotalDto { SubTotal = subTotal, IsUsingDefaultScoresheet = true };

                }
                else
                {
                    var questionIds = instance.Answers.Select(a => a.QuestionId).Distinct().ToList();

                    var numericSubtotal = await GetNumericAnswerSubtotal(instance, questionIds);
                    var yesNoSubtotal = await GetYesNoAnswerSubtotal(instance, questionIds);
                    var selectListSubtotal = await GetSelectListAnswerSubtotal(instance, questionIds);

                    double subTotal = numericSubtotal + yesNoSubtotal + selectListSubtotal;
                    return new SubTotalDto { SubTotal = subTotal, IsUsingDefaultScoresheet = false };

                }
            }
            else
            {
                double subTotal = (assessment.FinancialAnalysis ?? 0) + (assessment.EconomicImpact ?? 0) + (assessment.InclusiveGrowth ?? 0) + (assessment.CleanGrowth ?? 0);
                return new SubTotalDto { SubTotal = subTotal, IsUsingDefaultScoresheet = true };
            }
        }

        private async Task<double> GetSelectListAnswerSubtotal(ScoresheetInstanceDto instance, List<Guid> questionIds)
        {
            var existingSelectListQuestions = await _scoresheetAppService.GetSelectListQuestionsAsync(questionIds);
            var existingSelectListQuestionIds = existingSelectListQuestions.Select(a => a.Id).ToList();
            double selectListSubtotal = instance.Answers.Where(a => existingSelectListQuestionIds.Contains(a.QuestionId))
                .Sum(answer =>
                {
                    var value = ValueResolver.Resolve(answer.CurrentValue!, QuestionType.SelectList)!.ToString();
                    var question = existingSelectListQuestions.Find(q => q.Id == answer.QuestionId) ?? throw new AbpValidationException("Missing QuestionId");
                    var definition = JsonSerializer.Deserialize<QuestionSelectListDefinition>(question.Definition ?? "{}");
                    var selectedOption = definition?.Options.Find(o => o.Value == value);
                    if (selectedOption != null)
                    {
                        return selectedOption.NumericValue;
                    }
                    else
                    {
                        return 0;
                    }
                });
            return selectListSubtotal;
        }

        private async Task<double> GetYesNoAnswerSubtotal(ScoresheetInstanceDto instance, List<Guid> questionIds)
        {
            var existingYesNoQuestions = await _scoresheetAppService.GetYesNoQuestionsAsync(questionIds);
            var existingYesNoQuestionIds = existingYesNoQuestions.Select(a => a.Id).ToList();
            double yesNoSubtotal = instance.Answers.Where(a => existingYesNoQuestionIds.Contains(a.QuestionId))
                .Sum(answer =>
                {
                    var value = ValueResolver.Resolve(answer.CurrentValue!, QuestionType.YesNo)!.ToString();
                    var question = existingYesNoQuestions.Find(q => q.Id == answer.QuestionId) ?? throw new AbpValidationException("Missing QuestionId");
                    var definition = JsonSerializer.Deserialize<QuestionYesNoDefinition>(question.Definition ?? "{}");
                    return value switch
                    {
                        "Yes" => Convert.ToDouble(definition?.YesValue ?? 0),
                        "No" => Convert.ToDouble(definition?.NoValue ?? 0),
                        _ => 0,
                    };
                });
            return yesNoSubtotal;
        }

        private async Task<double> GetNumericAnswerSubtotal(ScoresheetInstanceDto instance, List<Guid> questionIds)
        {
            var existingNumericQuestionIds = await _scoresheetAppService.GetNumericQuestionIdsAsync(questionIds);
            double numericSubtotal = instance.Answers.Where(a => existingNumericQuestionIds.Contains(a.QuestionId))
                .Sum(a => Convert.ToDouble(ValueResolver.Resolve(a.CurrentValue!, QuestionType.Number)!.ToString()));
            return numericSubtotal;
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
                assessment.ApprovalRecommended = dto.ApprovalRecommended;
                await _assessmentRepository.UpdateAsync(assessment);
            }
        }

        #region ASSESSMENT COMMENTS
        public async Task<CommentDto> CreateCommentAsync(Guid id, CreateCommentDto dto)
        {
            return ObjectMapper.Map<AssessmentComment, CommentDto>((AssessmentComment)
             await _commentsManager.CreateCommentAsync(id, dto.Comment, CommentType.AssessmentComment));
        }

        public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid id)
        {
            return ObjectMapper.Map<IReadOnlyList<AssessmentComment>, IReadOnlyList<CommentDto>>((IReadOnlyList<AssessmentComment>)
                await _commentsManager.GetCommentsAsync(id, CommentType.AssessmentComment));
        }

        public async Task<CommentDto> UpdateCommentAsync(Guid id, UpdateCommentDto dto)
        {
            try
            {
                return ObjectMapper.Map<AssessmentComment, CommentDto>((AssessmentComment)
                      await _commentsManager.UpdateCommentAsync(id, dto.CommentId, dto.Comment, CommentType.AssessmentComment));
            }
            catch (EntityNotFoundException)
            {
                throw new InvalidCommentParametersException();
            }
        }

        public async Task<CommentDto> GetCommentAsync(Guid id, Guid commentId)
        {
            var comment = await _commentsManager.GetCommentAsync(id, commentId, CommentType.AssessmentComment);

            return comment == null
                ? throw new InvalidCommentParametersException()
                : ObjectMapper.Map<AssessmentComment, CommentDto>((AssessmentComment)comment);
        }
        #endregion ASSESSMENT COMMENTS

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
            var workflowActions = assessment.Workflow.GetPermittedActions();

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
        /// Generate a DOT graph from the Asssessment workflow.
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

            await AuthorizationService.CheckAsync(assessment, GetActionAuthorizationRequirement(triggerAction));

            await ApplyAdditionalValidationsAsync(assessmentId, triggerAction);

            await assessment.Workflow.ExecuteActionAsync(triggerAction);

            return ObjectMapper.Map<Assessment, AssessmentDto>(await _assessmentRepository.UpdateAsync(assessment, autoSave: true));
        }

        private async Task ApplyAdditionalValidationsAsync(Guid assessmentId, AssessmentAction triggerAction)
        {
            await ValidateValidScoresheetAsync(assessmentId, triggerAction);
        }

        private async Task ValidateValidScoresheetAsync(Guid assessmentId, AssessmentAction triggerAction)
        {
            if (await _featureChecker.IsEnabledAsync(UnityFlex) && triggerAction == AssessmentAction.Complete)
            {
                var requirementsMetResult = await _scoresheetInstanceAppService.ValidateAnswersAsync(assessmentId);

                if (requirementsMetResult?.Errors?.Count > 0)
                {
                    throw new InvalidScoresheetAnswersException([.. requirementsMetResult.Errors]);
                }
            }
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

        public async Task SaveScoresheetSectionAnswers(AssessmentScoreSectionDto dto)
        {
            var assessment = await _assessmentRepository.GetAsync(dto.AssessmentId);
            try
            {
                if (assessment != null)
                {
                    if (CurrentUser.GetId() != assessment.AssessorId)
                    {
                        throw new AbpValidationException("Error: You do not own this assessment record.");
                    }
                    if (assessment.Status.Equals(AssessmentState.COMPLETED))
                    {
                        throw new AbpValidationException("Error: This assessment is already completed.");
                    }

                    if (await _featureChecker.IsEnabledAsync(UnityFlex))
                    {
                        await _localEventBus.PublishAsync(new PersistScoresheetSectionInstanceEto()
                        {
                            AssessmentId = dto.AssessmentId,
                            AssessmentAnswers = dto.AssessmentAnswers
                        });
                    }
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

