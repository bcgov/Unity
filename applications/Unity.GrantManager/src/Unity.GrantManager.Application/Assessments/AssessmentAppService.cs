using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace Unity.GrantManager.Assessments
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(AssessmentAppService), typeof(IAssessmentAppService))]
    public class AssessmentAppService : ApplicationService, IAssessmentAppService
    {
        private readonly IAssessmentsRepository _assessmentsRepository;
        private readonly AssessmentManager _assessmentManager;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IIdentityUserLookupAppService _userLookupProvider;

        public AssessmentAppService(
            IAssessmentsRepository assessmentsRepository,
            AssessmentManager assessmentManager,
            IApplicationRepository applicationRepository,
            IIdentityUserLookupAppService userLookupProvider)
        {
            _assessmentsRepository = assessmentsRepository;
            _assessmentManager = assessmentManager;
            _applicationRepository = applicationRepository;
            _userLookupProvider = userLookupProvider;
        }

        public async Task<AssessmentDto> CreateAssessment(CreateAssessmentDto dto)
        {
            Application application = await _applicationRepository.GetAsync(dto.ApplicationId);
            IUserData currentUser = await _userLookupProvider.FindByIdAsync(CurrentUser.GetId());

            var result = await _assessmentManager.CreateAsync(application, currentUser);
            return ObjectMapper.Map<Assessment, AssessmentDto>(result);
        }

        public async Task<IList<AssessmentDto>> GetListAsync(Guid applicationId)
        {
            IQueryable<Assessment> queryableAssessments = _assessmentsRepository.GetQueryableAsync().Result;
            var comments = queryableAssessments.Where(c => c.ApplicationId.Equals(applicationId)).ToList();
            return await Task.FromResult<IList<AssessmentDto>>(ObjectMapper.Map<List<Assessment>, List<AssessmentDto>>(comments.OrderByDescending(s => s.CreationTime).ToList()));
        }

        public async Task<Guid?> GetCurrentUserAssessmentId(Guid applicationId)
        {
            var assessment = await _assessmentsRepository
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
            var assessment = await _assessmentsRepository.GetAsync(assessmentId);
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
            var assessment = await _assessmentsRepository.GetAsync(assessmentId);

            var authorizationAction = GrantApplicationPermissions.Assessments.Default + "." + triggerAction;
            await AuthorizationService.CheckAsync(assessment,
                new OperationAuthorizationRequirement { Name = authorizationAction });
            var newAssessment = await assessment.ExecuteActionAsync(triggerAction);
            return ObjectMapper.Map<Assessment, AssessmentDto>(await _assessmentsRepository.UpdateAsync(newAssessment, autoSave: true));
        }

        public async Task UpdateAssessmentRecommendation(UpdateAssessmentRecommendationDto dto)
        {
            try
            {
                var assessment = await _assessmentsRepository.GetAsync(dto.AssessmentId);
                if (assessment != null)
                {
                    assessment.ApprovalRecommended = dto.ApprovalRecommended;
                    await _assessmentsRepository.UpdateAsync(assessment);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating  assessment recommendation");
            }
        }
    }
}
