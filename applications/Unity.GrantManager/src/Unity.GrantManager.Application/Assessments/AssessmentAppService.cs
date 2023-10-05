using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Comments;
using Unity.GrantManager.Exceptions;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.Workflow;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Identity;
using Volo.Abp.Users;

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
        private readonly IIdentityUserLookupAppService _userLookupProvider;
        private readonly ICommentsManager _commentsManager;

        public AssessmentAppService(
            IAssessmentRepository assessmentRepository,
            AssessmentManager assessmentManager,
            IApplicationRepository applicationRepository,
            IIdentityUserLookupAppService userLookupProvider,
            ICommentsManager commentsManager)
        {
            _assessmentRepository = assessmentRepository;
            _assessmentManager = assessmentManager;
            _applicationRepository = applicationRepository;
            _userLookupProvider = userLookupProvider;
            _commentsManager = commentsManager;
        }

        public async Task<AssessmentDto> CreateAsync(CreateAssessmentDto dto)
        {
            Application application = await _applicationRepository.GetAsync(dto.ApplicationId);
            IUserData currentUser = await _userLookupProvider.FindByIdAsync(CurrentUser.GetId());

            var result = await _assessmentManager.CreateAsync(application, currentUser);
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
            return ObjectMapper.Map<
                List<AssessmentWithAssessorQueryResultItem>,
                List<AssessmentListItemDto>>
                (await _assessmentRepository.GetListWithAssessorsAsync(applicationId));
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
        /// Get all permitted actions for an Assessment given it's state.
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
    }
}