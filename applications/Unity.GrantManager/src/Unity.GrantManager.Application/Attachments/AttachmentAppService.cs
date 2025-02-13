using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Intakes;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Attachments;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(AttachmentAppService), typeof(IAttachmentAppService))]
public class AttachmentAppService(
    IApplicationAttachmentRepository applicationAttachmentRepository,
    IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
    IAssessmentAttachmentRepository assessmentAttachmentRepository,
    IIntakeFormSubmissionManager intakeFormSubmissionManager,
    IPersonRepository personUserRepository) : ApplicationService, IAttachmentAppService
{
    public async Task<IList<ApplicationAttachmentDto>> GetApplicationAsync(Guid applicationId)
    {
        var query = from applicationAttachment in await applicationAttachmentRepository.GetQueryableAsync()
                    join person in await personUserRepository.GetQueryableAsync() on applicationAttachment.UserId equals person.Id
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
        var query = from applicationAttachment in await assessmentAttachmentRepository.GetQueryableAsync()
                    join person in await personUserRepository.GetQueryableAsync() on applicationAttachment.UserId equals person.Id
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
        return await applicationChefsFileAttachmentRepository.GetListAsync(applicationId);
    }

    public async Task ResyncSubmissionAttachmentsAsync(Guid applicationId)
    {
        await intakeFormSubmissionManager.ResyncSubmissionAttachments(applicationId);
    }

    public async Task<AttachmentMetadataDto> GetAttachmentMetadataAsync(AttachmentType attachmentType, Guid attachmentId)
    {
        return attachmentType switch
        {
            AttachmentType.Application => await GetMetadataInternalAsync(
                attachmentId, applicationAttachmentRepository),
            AttachmentType.Assessment => await GetMetadataInternalAsync(
                attachmentId, assessmentAttachmentRepository),
            AttachmentType.CHEFS => await GetMetadataInternalAsync(
                attachmentId, applicationChefsFileAttachmentRepository),
            _ => throw new ArgumentException("Invalid attachment type", nameof(attachmentType)),
        };
    }

    protected internal static async Task<AttachmentMetadataDto> GetMetadataInternalAsync<T>(
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
            AttachmentType.Application => await UpdateMetadataInternalAsync(
                updateAttachment,
                applicationAttachmentRepository,
                AttachmentType.Application),
            AttachmentType.Assessment => await UpdateMetadataInternalAsync(
                updateAttachment,
                assessmentAttachmentRepository,
                AttachmentType.Assessment),
            AttachmentType.CHEFS => await UpdateMetadataInternalAsync(
                updateAttachment,
                applicationChefsFileAttachmentRepository,
                AttachmentType.CHEFS),
            _ => throw new ArgumentException("Invalid attachment type", nameof(updateAttachment)),
        };
    }

    protected internal static async Task<AttachmentMetadataDto> UpdateMetadataInternalAsync<T>(
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
