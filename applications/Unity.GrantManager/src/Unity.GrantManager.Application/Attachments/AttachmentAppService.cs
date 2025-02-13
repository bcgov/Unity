using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Intakes;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

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
        private readonly IApplicationChefsFileAttachmentRepository _applicationChefsFileAttachmentRepository;
        private readonly IIntakeFormSubmissionManager _intakeFormSubmissionManager;

        public AttachmentService(IApplicationAttachmentRepository applicationAttachmentRepository,
            IAssessmentAttachmentRepository assessmentAttachmentRepository,
            IPersonRepository personUserRepository,
            IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
            IIntakeFormSubmissionManager intakeFormSubmissionManager)
        {
            _applicationAttachmentRepository = applicationAttachmentRepository;
            _assessmentAttachmentRepository = assessmentAttachmentRepository;
            _personRepository = personUserRepository;
            _applicationChefsFileAttachmentRepository = applicationChefsFileAttachmentRepository;
            _intakeFormSubmissionManager = intakeFormSubmissionManager;
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
                            Time = applicationAttachment.Time,
                            CreatorId = person.Id
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
                            Time = applicationAttachment.Time,
                            CreatorId = person.Id
                        };

            return query.ToList();
        }

        public async Task<List<ApplicationChefsFileAttachment>> GetApplicationChefsFileAttachmentsAsync(Guid applicationId)
        {
            return await _applicationChefsFileAttachmentRepository.GetListAsync(applicationId);
        }

        public async Task ResyncSubmissionAttachmentsAsync(Guid applicationId)
        {
            await _intakeFormSubmissionManager.ResyncSubmissionAttachments(applicationId);
        }

        public async Task<AttachmentMetadataDto> GetAttachmentMetadataAsync(AttachmentType attachmentType, Guid attachmentId)
        {
            return attachmentType switch
            {
                AttachmentType.Application => await GetMetadataInternalAsync(
                    attachmentId, _applicationAttachmentRepository),
                AttachmentType.Assessment => await GetMetadataInternalAsync(
                    attachmentId, _assessmentAttachmentRepository),
                AttachmentType.CHEFS => await GetMetadataInternalAsync(
                    attachmentId, _applicationChefsFileAttachmentRepository),
                _ => throw new ArgumentException("Invalid attachment type", nameof(attachmentType)),
            };
        }

        protected async Task<AttachmentMetadataDto> GetMetadataInternalAsync<T>(
            Guid attachmentId, 
            IRepository<T, Guid> repository) where T : AbstractAttachmentBase
        {
            var attachment = await repository.GetAsync(attachmentId) ?? throw new EntityNotFoundException();
            return new AttachmentMetadataDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                DisplayName = attachment.DisplayName,
                CreatorId = GetCreatorId(attachment),
                AttachmentType = attachment.AttachmentType
            };
        }

        public async Task<AttachmentMetadataDto> UpdateAttachmentMetadataAsync(UpdateAttachmentMetadataDto updateAttachment)
        {
            return updateAttachment.AttachmentType switch
            {
                AttachmentType.Application => await UpdateAttachmentAsync(
                    updateAttachment,
                    _applicationAttachmentRepository,
                    AttachmentType.Application),
                AttachmentType.Assessment => await UpdateAttachmentAsync(
                    updateAttachment,
                    _assessmentAttachmentRepository,
                    AttachmentType.Assessment),
                AttachmentType.CHEFS => await UpdateAttachmentAsync(
                    updateAttachment,
                    _applicationChefsFileAttachmentRepository,
                    AttachmentType.CHEFS),
                _ => throw new ArgumentException("Invalid attachment type", nameof(updateAttachment.AttachmentType)),
            };
        }

        protected async Task<AttachmentMetadataDto> UpdateAttachmentAsync<T>(
            UpdateAttachmentMetadataDto updateAttachment,
            IRepository<T, Guid> repository,
            AttachmentType attachmentType) where T : AbstractAttachmentBase
        {
            var attachment = await repository.GetAsync(updateAttachment.Id) ?? throw new EntityNotFoundException();
            
            // Properties to be updated
            attachment.DisplayName = updateAttachment.DisplayName;

            var updatedAttachment = await repository.UpdateAsync(attachment, autoSave: true) ?? throw new EntityNotFoundException();
            return new AttachmentMetadataDto
            {
                Id = updatedAttachment.Id,
                FileName = updatedAttachment.FileName,
                DisplayName = updatedAttachment.DisplayName,
                CreatorId = GetCreatorId(updatedAttachment),
                AttachmentType = attachmentType
            };
        }

        private static Guid? GetCreatorId<T>(T attachment) where T : AbstractAttachmentBase
        {
            var property = typeof(T).GetProperty("CreatorId");
            return property?.GetValue(attachment) as Guid?;
        }

    }
}
