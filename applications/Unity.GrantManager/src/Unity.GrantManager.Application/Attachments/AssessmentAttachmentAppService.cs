using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.GrantApplications
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(AssessmentAttachmentService), typeof(IAssessmentAttachmentService))]
    public class AssessmentAttachmentService : ApplicationService, IAssessmentAttachmentService
    {
        private readonly IAssessmentAttachmentRepository _assessmentAttachmentRepository;

        public AssessmentAttachmentService(IAssessmentAttachmentRepository assessmentAttachmentRepository)
        {
            _assessmentAttachmentRepository = assessmentAttachmentRepository;
        }

        public async Task<IList<AssessmentAttachmentDto>> GetListAsync(Guid assessmentId)
        {
            IQueryable<AssessmentAttachment> queryableAttachment = _assessmentAttachmentRepository.GetQueryableAsync().Result; 
            var attachments  = queryableAttachment.Where(c => c.AssessmentId.Equals(assessmentId)).ToList();
            return await Task.FromResult<IList<AssessmentAttachmentDto>>(ObjectMapper.Map<List<AssessmentAttachment>, List<AssessmentAttachmentDto>>(attachments.OrderByDescending(s => s.Time).ToList()));
        }


    }
}
