using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Application.Services;
using System.Collections.Generic;
using System.Linq;
using Unity.GrantManager.Applications;
using Volo.Abp;
using Volo.Abp.Users;
using Unity.GrantManager.Permissions;
using Volo.Abp.Identity;
using Volo.Abp.Domain.Entities;

namespace Unity.GrantManager.Assessments
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(AssessmentsAppService), typeof(IAssessmentsService))]
    public class AssessmentsAppService : ApplicationService, IAssessmentsService
    {
        private readonly IAssessmentsRepository _assessmentsRepository;
        private readonly AssessmentManager _assessmentManager;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IIdentityUserLookupAppService _userLookupProvider;

        public AssessmentsAppService(
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

        [Authorize(GrantApplicationPermissions.Adjudications.Start)]
        public async Task<AssessmentDto> CreateAssessment(CreateAssessmentDto dto)
        {
            Application application = await _applicationRepository.GetAsync(dto.ApplicationId);
            IUserData currentUser = await _userLookupProvider.FindByIdAsync(CurrentUser.GetId());

            var result =  await _assessmentManager.CreateAsync(application, currentUser);
            return ObjectMapper.Map<Assessment, AssessmentDto>(result);
        }

        public async Task<IList<AssessmentDto>> GetListAsync(Guid applicationId)
        {
            IQueryable<Assessment> queryableAssessments = _assessmentsRepository.GetQueryableAsync().Result;
            var comments = queryableAssessments.Where(c => c.ApplicationId.Equals(applicationId)).ToList();
            return await Task.FromResult<IList<AssessmentDto>>(ObjectMapper.Map<List<Assessment>, List<AssessmentDto>>(comments.OrderByDescending(s => s.CreationTime).ToList()));
        }

        public async Task<List<AssessmentAction>> GetAvailableActions(Guid assessmentId)
        {
            var assessment = await _assessmentsRepository.GetAsync(assessmentId);
            return assessment.GetActions().ToList();
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
                // TODO: Exception handling
            }
        }
    }
}
