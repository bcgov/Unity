using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.GrantApplications
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(AttachmentService), typeof(IAttachmentService))]
    public class AttachmentService : ApplicationService, IAttachmentService
    {
        private readonly IApplicationAttachmentRepository _applicationAttachmentRepository;
        private readonly IAssessmentAttachmentRepository _assessmentAttachmentRepository;

        public AttachmentService(IApplicationAttachmentRepository applicationAttachmentRepository, IAssessmentAttachmentRepository assessmentAttachmentRepository)
        {
            _applicationAttachmentRepository = applicationAttachmentRepository;
            _assessmentAttachmentRepository = assessmentAttachmentRepository;
        }

        public async Task<IList<ApplicationAttachmentDto>> GetApplicationAsync(Guid applicationId)
        {
            IQueryable<ApplicationAttachment> queryableAttachment = _applicationAttachmentRepository.GetQueryableAsync().Result;
            var attachments  = queryableAttachment.Where(c => c.ApplicationId.Equals(applicationId)).ToList();
            return await Task.FromResult<IList<ApplicationAttachmentDto>>(ObjectMapper.Map<List<ApplicationAttachment>, List<ApplicationAttachmentDto>>(attachments.OrderByDescending(s => s.Time).ToList()));
        }

        public async Task<IList<AssessmentAttachmentDto>> GetAssessmentAsync(Guid assessmentId)
        {
            IQueryable<AssessmentAttachment> queryableAttachment = _assessmentAttachmentRepository.GetQueryableAsync().Result;
            var attachments = queryableAttachment.Where(c => c.AssessmentId.Equals(assessmentId)).ToList();
            return await Task.FromResult<IList<AssessmentAttachmentDto>>(ObjectMapper.Map<List<AssessmentAttachment>, List<AssessmentAttachmentDto>>(attachments.OrderByDescending(s => s.Time).ToList()));
        }
    }
}
