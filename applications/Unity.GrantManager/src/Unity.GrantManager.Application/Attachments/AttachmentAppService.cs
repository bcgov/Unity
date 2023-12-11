using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Identity;
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
        private readonly IPersonRepository _personRepository;

        public AttachmentService(IApplicationAttachmentRepository applicationAttachmentRepository, 
            IAssessmentAttachmentRepository assessmentAttachmentRepository,
            IPersonRepository personUserRepository)
        {
            _applicationAttachmentRepository = applicationAttachmentRepository;
            _assessmentAttachmentRepository = assessmentAttachmentRepository;
            _personRepository = personUserRepository;
        }

        public async Task<IList<ApplicationAttachmentDto>> GetApplicationAsync(Guid applicationId)
        {
            var query = from applicationAttachment in await _applicationAttachmentRepository.GetQueryableAsync()
                        join person in await _personRepository.GetQueryableAsync() on applicationAttachment.UserId equals person.Id
                        where applicationAttachment.ApplicationId == applicationId
                        select new ApplicationAttachmentDto()
                        {
                            AttachedBy = person.FullName,
                            Id = applicationAttachment.Id,
                            FileName = applicationAttachment.FileName,
                            S3ObjectKey = applicationAttachment.S3ObjectKey,
                            Time = applicationAttachment.Time
                        };

            return query.ToList();
        }

        public async Task<IList<AssessmentAttachmentDto>> GetAssessmentAsync(Guid assessmentId)
        {
            var query = from applicationAttachment in await _assessmentAttachmentRepository.GetQueryableAsync()
                        join person in await _personRepository.GetQueryableAsync() on applicationAttachment.UserId equals person.Id
                        where applicationAttachment.AssessmentId == assessmentId
                        select new AssessmentAttachmentDto()
                        {
                            AttachedBy = person.FullName,
                            Id = applicationAttachment.Id,
                            FileName = applicationAttachment.FileName,
                            S3ObjectKey = applicationAttachment.S3ObjectKey,
                            Time = applicationAttachment.Time
                        };

            return query.ToList();
        }
    }
}
