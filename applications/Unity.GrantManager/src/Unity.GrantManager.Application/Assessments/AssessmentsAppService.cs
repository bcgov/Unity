using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Application.Services;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GrantManager.Assessments
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(AssessmentsAppService), typeof(IAssessmentsService))]
    public class AssessmentsAppService : ApplicationService, IAssessmentsService
    {
        private readonly IAssessmentsRepository _assessmentsRepository;

        public AssessmentsAppService(IAssessmentsRepository assessmentsRepository)
        {
            _assessmentsRepository = assessmentsRepository;
        }

        public async Task<AssessmentDto> CreateAssessment(CreateAssessmentDto dto)
        {
            try
            {
                return ObjectMapper.Map<Assessment, AssessmentDto>(await _assessmentsRepository.InsertAsync(
                    new Assessment
                    {
                        ApplicationId = dto.ApplicationId
                    },
                    autoSave: true
                ));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating assessment");
                return new AssessmentDto();
                // TODO: Exception handling
            }
        }

        public async Task<IList<AssessmentDto>> GetListAsync(Guid applicationId)
        {
            IQueryable<Assessment> queryableAssessments = _assessmentsRepository.GetQueryableAsync().Result;
            var comments = queryableAssessments.Where(c => c.ApplicationId.Equals(applicationId)).ToList();
            return await Task.FromResult<IList<AssessmentDto>>(ObjectMapper.Map<List<Assessment>, List<AssessmentDto>>(comments.OrderByDescending(s => s.CreationTime).ToList()));
        }
    }
}
