using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Extraction;
using Unity.AI.Requests;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Operations;

public class AttachmentSummaryService(
    IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
    ISubmissionAppService submissionAppService,
    ITextExtractionService textExtractionService,
    IAIService aiService,
    ILogger<AttachmentSummaryService> logger) : IAttachmentSummaryService, ITransientDependency
{
    private const string SummaryGenerationFailedMessage = "AI summary generation failed.";

    public async Task<string> GenerateAndSaveAsync(Guid attachmentId, string? promptVersion = null)
    {
        var attachment = await applicationChefsFileAttachmentRepository.GetAsync(attachmentId);
        var fileName = string.IsNullOrWhiteSpace(attachment.FileName) ? "unknown" : attachment.FileName;

        await using var attachmentStream = await OpenAttachmentStreamAsync(attachment, fileName);
        var extractedText = await textExtractionService.ExtractTextAsync(fileName, attachmentStream.Content, attachmentStream.ContentType);

        var summaryResponse = await aiService.GenerateAttachmentSummaryAsync(new AttachmentSummaryRequest
        {
            FileName = fileName,
            ContentType = attachmentStream.ContentType,
            ExtractedText = extractedText,
            PromptVersion = promptVersion,
        });

        attachment.AISummary = summaryResponse.Summary;
        await applicationChefsFileAttachmentRepository.UpdateAsync(attachment);

        return summaryResponse.Summary;
    }

    public async Task<List<string>> GenerateAndSaveAsync(IEnumerable<Guid> attachmentIds, string? promptVersion = null)
    {
        var summaries = new List<string>();

        foreach (var attachmentId in attachmentIds)
        {
            try
            {
                summaries.Add(await GenerateAndSaveAsync(attachmentId, promptVersion));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating AI summary for attachment {AttachmentId}", attachmentId);
                summaries.Add(SummaryGenerationFailedMessage);
            }
        }

        return summaries;
    }

    public async Task<List<string>> GenerateForApplicationAsync(Guid applicationId, string? promptVersion = null)
    {
        var attachmentIds = (await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId))
            .Select(a => a.Id)
            .ToList();

        return await GenerateAndSaveAsync(attachmentIds, promptVersion);
    }

    private async Task<ChefsFileAttachmentStream> OpenAttachmentStreamAsync(ApplicationChefsFileAttachment attachment, string fileName)
    {
        if (!Guid.TryParse(attachment.ChefsSubmissionId, out var submissionId) ||
            !Guid.TryParse(attachment.ChefsFileId, out var fileId))
        {
            logger.LogWarning(
                "Attachment {AttachmentId} has invalid CHEFS IDs. Falling back to metadata-only summary generation.",
                attachment.Id);
            return ChefsFileAttachmentStream.Empty;
        }

        try
        {
            var stream = await submissionAppService.GetChefsFileAttachmentStream(submissionId, fileId, fileName);
            return stream ?? ChefsFileAttachmentStream.Empty;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed retrieving CHEFS content for attachment {AttachmentId}. Falling back to metadata-only summary generation.",
                attachment.Id);
            return ChefsFileAttachmentStream.Empty;
        }
    }
}
