using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.AI;
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
    IPersonRepository personUserRepository, 
    IAIService aiService,
    ISubmissionAppService submissionAppService) : ApplicationService, IAttachmentAppService
{
    public async Task<IList<ApplicationAttachmentDto>> GetApplicationAsync(Guid applicationId)
    {
        return (await GetAttachmentsAsync(new AttachmentParametersDto(AttachmentType.APPLICATION, applicationId)))
            .Select(attachment => new ApplicationAttachmentDto
            {
                AttachedBy  = attachment.AttachedBy,
                Id          = attachment.Id,
                FileName    = attachment.FileName,
                S3ObjectKey = attachment.S3ObjectKey,
                Time        = attachment.Time,
                CreatorId   = attachment.CreatorId,
                DisplayName = attachment.DisplayName
            }).ToList();
    }

    public async Task<IList<AssessmentAttachmentDto>> GetAssessmentAsync(Guid assessmentId)
    {
        return (await GetAttachmentsAsync(new AttachmentParametersDto(AttachmentType.ASSESSMENT, assessmentId)))
            .Select(attachment => new AssessmentAttachmentDto
            {
                AttachedBy  = attachment.AttachedBy,
                Id          = attachment.Id,
                FileName    = attachment.FileName,
                S3ObjectKey = attachment.S3ObjectKey,
                Time        = attachment.Time,
                CreatorId   = attachment.CreatorId,
                DisplayName = attachment.DisplayName
            }).ToList();
    }

    public async Task<List<ApplicationChefsFileAttachment>> GetApplicationChefsFileAttachmentsAsync(Guid applicationId)
    {
        return await applicationChefsFileAttachmentRepository.GetListAsync(applicationId);
    }

    public async Task ResyncSubmissionAttachmentsAsync(Guid applicationId)
    {
        await intakeFormSubmissionManager.ResyncSubmissionAttachments(applicationId);
    }

    public async Task<IList<UnityAttachmentDto>> GetAttachmentsAsync(AttachmentParametersDto attachmentParametersDto)
    {
        if (attachmentParametersDto.AttachedResourceId == Guid.Empty)
        {
            return [];
        }

        return attachmentParametersDto.AttachmentType switch
        {
            AttachmentType.APPLICATION => await GetAttachmentsInternalAsync(
                applicationAttachmentRepository,
                attachment => attachment.ApplicationId == attachmentParametersDto.AttachedResourceId),
            AttachmentType.ASSESSMENT => await GetAttachmentsInternalAsync(
                assessmentAttachmentRepository,
                attachment => attachment.AssessmentId == attachmentParametersDto.AttachedResourceId),
            _ => throw new ArgumentException("Attachment type is not supported", nameof(attachmentParametersDto)),
        };
    }

    protected internal async Task<IList<UnityAttachmentDto>> GetAttachmentsInternalAsync<T>(
        IRepository<T, Guid> repository,
        Func<T, bool> predicate) where T : AbstractS3Attachment
    {
        var attachments = await repository.GetQueryableAsync();
        var people = await personUserRepository.GetQueryableAsync();
        var query = from attachment in attachments.AsEnumerable()
                    join person in people.AsEnumerable() on attachment.UserId equals person.Id
                    where predicate(attachment)
                    select new UnityAttachmentDto()
                    {
                        Id             = attachment.Id,
                        FileName       = attachment.FileName,
                        DisplayName    = attachment.DisplayName,
                        S3ObjectKey    = attachment.S3ObjectKey,
                        Time           = attachment.Time,
                        AttachmentType = attachment.AttachmentType,
                        AttachedBy     = person.FullName,
                        CreatorId      = person.Id
                    };

        return query.ToList();
    }

    public async Task<AttachmentMetadataDto> GetAttachmentMetadataAsync(AttachmentType attachmentType, Guid attachmentId)
    {
        return attachmentType switch
        {
            AttachmentType.APPLICATION => await GetMetadataInternalAsync(
                attachmentId, applicationAttachmentRepository),
            AttachmentType.ASSESSMENT => await GetMetadataInternalAsync(
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
            Id             = attachment.Id,
            FileName       = attachment.FileName,
            DisplayName    = attachment.DisplayName,
            CreatorId      = GetCreatorId(attachment),
            AttachmentType = attachment.AttachmentType
        };
    }

    public async Task<AttachmentMetadataDto> UpdateAttachmentMetadataAsync(UpdateAttachmentMetadataDto updateAttachment)
    {
        return updateAttachment.AttachmentType switch
        {
            AttachmentType.APPLICATION => await UpdateMetadataInternalAsync(
                updateAttachment,
                applicationAttachmentRepository,
                AttachmentType.APPLICATION),
            AttachmentType.ASSESSMENT => await UpdateMetadataInternalAsync(
                updateAttachment,
                assessmentAttachmentRepository,
                AttachmentType.ASSESSMENT),
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
            Id             = updatedAttachment.Id,
            FileName       = updatedAttachment.FileName,
            DisplayName    = updatedAttachment.DisplayName,
            CreatorId      = GetCreatorId(updatedAttachment),
            AttachmentType = attachmentType
        };
    }

    private static Guid? GetCreatorId<T>(T attachment) where T : AbstractAttachmentBase
    {
        var property = typeof(T).GetProperty("CreatorId");
        return property?.GetValue(attachment) as Guid?;
    }

    public async Task<string> GenerateAISummaryAttachmentAsync(Guid attachmentId)
    {
        // Get the attachment
        var attachment = await applicationChefsFileAttachmentRepository.GetAsync(attachmentId);
        
        // Get file content from CHEFS
        var fileDto = await submissionAppService.GetChefsFileAttachment(
            Guid.Parse(attachment.ChefsSumbissionId ?? ""),
            Guid.Parse(attachment.ChefsFileId ?? ""),
            attachment.FileName ?? "");
        
        if (fileDto?.Content == null)
        {
            return "Unable to retrieve file content for AI analysis.";
        }
        
        // Generate AI summary
        var summary = await aiService.GenerateAttachmentSummaryAsync(
            attachment.FileName ?? "",
            fileDto.Content,
            fileDto.ContentType);
        
        // Update attachment with summary
        attachment.AISummary = summary;
        await applicationChefsFileAttachmentRepository.UpdateAsync(attachment);
        
        return summary;
    }
    
    public async Task<List<string>> GenerateAISummariesAttachmentsAsync(List<Guid> attachmentIds)
    {
        var summaries = new List<string>();
        
        foreach (var attachmentId in attachmentIds)
        {
            try
            {
                var summary = await GenerateAISummaryAttachmentAsync(attachmentId);
                summaries.Add(summary);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating AI summary for attachment {AttachmentId}", attachmentId);
                summaries.Add($"Error generating summary: {ex.Message}");
            }
        }
        
        return summaries;
    }

}
