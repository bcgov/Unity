using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Scoresheets;
using Unity.Flex.Scoresheets.Events;
using Unity.Flex.Worksheets.Values;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Comments;
using Unity.GrantManager.Exceptions;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.Workflow;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
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
        private readonly ILocalEventBus _localEventBus;
        private readonly IFeatureChecker _featureChecker;

        public AssessmentAppService(
            IAssessmentRepository assessmentRepository,
            AssessmentManager assessmentManager,
            IApplicationRepository applicationRepository,
            IIdentityUserIntegrationService userLookupProvider,
            ICommentsManager commentsManager,
            IScoresheetInstanceAppService scoresheetInstanceAppService,
            IFeatureChecker featureChecker,
            ILocalEventBus localEventBus)
        {
            _assessmentRepository = assessmentRepository;
            _assessmentManager = assessmentManager;
            _applicationRepository = applicationRepository;
            _userLookupProvider = userLookupProvider;
            _commentsManager = commentsManager;
            _scoresheetInstanceAppService = scoresheetInstanceAppService;
            _featureChecker = featureChecker;
            _localEventBus = localEventBus;
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

        public async Task<List<AssessmentListItemDto>> GetDisplayList(Guid applicationId)
        {
            var assessmentList = ObjectMapper.Map<
                List<AssessmentWithAssessorQueryResultItem>,
                List<AssessmentListItemDto>>
                (await _assessmentRepository.GetListWithAssessorsAsync(applicationId));
            foreach (var assessment in assessmentList)
            {
                assessment.SubTotal = await GetSubTotal(assessment);
            }

            return assessmentList;
        }

        private async Task<double> GetSubTotal(AssessmentListItemDto assessment)
        {
            if (await _featureChecker.IsEnabledAsync("Unity.Flex"))
            {
                var instance = await _scoresheetInstanceAppService.GetByCorrelationAsync(assessment.Id);

                if (instance == null)
                {
                    return (assessment.SectionScore1 ?? 0) + (assessment.SectionScore2 ?? 0) + (assessment.SectionScore3 ?? 0) + (assessment.SectionScore4 ?? 0);
                }
                else
                {
                    return instance.Answers.Sum(a => Convert.ToDouble(ValueResolver.Resolve(a.CurrentValue!, Unity.Flex.Worksheets.CustomFieldType.Numeric)!.ToString()));
                }
            }
            else
            {
                return (assessment.SectionScore1 ?? 0) + (assessment.SectionScore2 ?? 0) + (assessment.SectionScore3 ?? 0) + (assessment.SectionScore4 ?? 0);
            }
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
            return blankAssessment.Workflow.GetAllActions().ToList();
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

            await assessment.Workflow.ExecuteActionAsync(triggerAction);

            return ObjectMapper.Map<Assessment, AssessmentDto>(await _assessmentRepository.UpdateAsync(assessment, autoSave: true));
        }

        private static OperationAuthorizationRequirement GetActionAuthorizationRequirement(AssessmentAction triggerAction)
        {
            return new OperationAuthorizationRequirement { Name = $"{GrantApplicationPermissions.Assessments.Default}.{triggerAction}" };
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
                    assessment.SectionScore1 = dto.SectionScore1;
                    assessment.SectionScore2 = dto.SectionScore2;
                    assessment.SectionScore3 = dto.SectionScore3;
                    assessment.SectionScore4 = dto.SectionScore4;
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

        public async Task SaveScoresheetAnswer(Guid assessmentId, Guid questionId, double answer)
        {
            var assessment = await _assessmentRepository.GetAsync(assessmentId);
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

                if (await _featureChecker.IsEnabledAsync("Unity.Flex"))
                {
                    await _localEventBus.PublishAsync(new PersistScoresheetInstanceEto()
                    {
                        CorrelationId = assessmentId,
                        QuestionId = questionId,
                        Answer = answer
                    });
                }
            }
            else
            {
                throw new AbpValidationException("AssessmentId Not Found: " + assessmentId + ".");
            }
        }
    }
}