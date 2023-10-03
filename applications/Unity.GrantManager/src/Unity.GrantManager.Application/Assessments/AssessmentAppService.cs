using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Comments;
using Unity.GrantManager.Exceptions;
using Unity.GrantManager.Permissions;
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
        private readonly ICurrentUser _currentUser;

        public AssessmentAppService(
            IAssessmentRepository assessmentRepository,
            AssessmentManager assessmentManager,
            IApplicationRepository applicationRepository,
            IIdentityUserLookupAppService userLookupProvider,
            ICommentsManager commentsManager,
            ICurrentUser currentUser)
        {
            _assessmentRepository = assessmentRepository;
            _assessmentManager = assessmentManager;
            _applicationRepository = applicationRepository;
            _userLookupProvider = userLookupProvider;
            _commentsManager = commentsManager;
            _currentUser = currentUser;
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
            var comments = queryableAssessments.Where(c => c.ApplicationId.Equals(applicationId)).ToList();
            return await Task.FromResult<IList<AssessmentDto>>(ObjectMapper.Map<List<Assessment>, List<AssessmentDto>>(comments.OrderByDescending(s => s.CreationTime).ToList()));
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

        /* Assessment Comments */
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

        /* Workflows */
        public async Task<Guid?> GetCurrentUserAssessmentId(Guid applicationId)
        {
            var assessment = await _assessmentRepository
                .FindAsync(x => x.ApplicationId == applicationId && x.AssignedUserId == CurrentUser.GetId());
            return assessment?.Id;
        }

        public List<string?> GetAllActions()
        {
            // NOTE: Replace with static wokflow class
            var blankAssessment = new Assessment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var allTriggers = blankAssessment.GetAllWorkflowActions();
            return allTriggers.ToList();
        }

        public static string? GetWorkflowDiagram()
        {
            // NOTE: Replace with static wokflow class
            var blankAssessment = new Assessment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            return blankAssessment.GetWorkflowDiagram();
        }

        public async Task<List<AssessmentAction>> GetAvailableActions(Guid assessmentId)
        {
            var assessment = await _assessmentRepository.GetAsync(assessmentId);
            var workflowActions = assessment.GetActions();

            List<AssessmentAction> permittedActions = new();
            foreach (var action in workflowActions)
            {
                var currentRequirement = new OperationAuthorizationRequirement { Name = GrantApplicationPermissions.Assessments.Default + "." + action };
                if (await AuthorizationService.IsGrantedAsync(assessment, currentRequirement))
                {
                    permittedActions.Add(action);
                }
            }

            return permittedActions;
        }

        public async Task<AssessmentDto> ExecuteAssessmentAction(Guid assessmentId, AssessmentAction triggerAction = AssessmentAction.SendToTeamLead)
        {
            var assessment = await _assessmentRepository.GetAsync(assessmentId);

            var authorizationAction = GrantApplicationPermissions.Assessments.Default + "." + triggerAction;
            await AuthorizationService.CheckAsync(assessment,
                new OperationAuthorizationRequirement { Name = authorizationAction });
            var newAssessment = await assessment.ExecuteActionAsync(triggerAction);
            return ObjectMapper.Map<Assessment, AssessmentDto>(await _assessmentRepository.UpdateAsync(newAssessment, autoSave: true));
        }
    }
}