using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Extraction;
using Unity.AI.Localization;
using Unity.AI.Requests;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Operations;

public class AttachmentSummaryService(
    IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
    IChefsFileAttachmentStreamProvider chefsFileAttachmentStreamProvider,
    ITextExtractionService textExtractionService,
    IAIService aiService,
    IAIGenerationPrerequisiteValidator aiGenerationPrerequisiteValidator,
    AIExecutionModeResolver executionModeResolver,
    ILogger<AttachmentSummaryService> logger,
    IStringLocalizer<AIResource> localizer) : IAttachmentSummaryService, ITransientDependency
{
    private const string SummaryGenerationFailedMessage = "AI summary generation failed.";

    public async Task<string> GenerateAndSaveAsync(Guid attachmentId, string? promptVersion = null, CancellationToken cancellationToken = default)
    {
        var attachment = await applicationChefsFileAttachmentRepository.GetAsync(attachmentId);
        var fileName = string.IsNullOrWhiteSpace(attachment.FileName) ? "unknown" : attachment.FileName;

        await using var attachmentStream = await OpenAttachmentStreamAsync(attachment, fileName, cancellationToken);
        var extractedText = await textExtractionService.ExtractTextAsync(fileName, attachmentStream.Content, attachmentStream.ContentType, cancellationToken);

        var summaryResponse = await aiService.GenerateAttachmentSummaryAsync(new AttachmentSummaryRequest
        {
            FileName = fileName,
            ContentType = attachmentStream.ContentType,
            ExtractedText = extractedText,
            PromptVersion = promptVersion,
        }, cancellationToken);

        attachment.AISummary = summaryResponse.Summary;
        await applicationChefsFileAttachmentRepository.UpdateAsync(attachment);

        return summaryResponse.Summary;
    }

    public async Task<List<string>> GenerateAndSaveAsync(IEnumerable<Guid> attachmentIds, string? promptVersion = null, CancellationToken cancellationToken = default)
    {
        var ids = attachmentIds as IReadOnlyCollection<Guid> ?? attachmentIds.ToList();
        if (ids.Count == 0)
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.SelectAttachmentForSummaries]);
        }

        var mode = executionModeResolver.ResolveMode(AIExecutionModeResolver.AttachmentSummaryOperation);
        if (mode != AIExecutionMode.Sequential)
        {
            logger.LogWarning(
                "AI attachment summary {ExecutionMode} mode is not supported by the current repository-backed execution path. Falling back to sequential execution.",
                mode);
            mode = AIExecutionMode.Sequential;
        }

        return await AIExecutionStrategy.RunAsync(
            ids,
            mode,
            id => GenerateOrFallbackAsync(id, promptVersion, cancellationToken),
            batch => GenerateSequentiallyAsync(batch, promptVersion, cancellationToken));
    }

    private async Task<List<string>> GenerateSequentiallyAsync(
        IReadOnlyCollection<Guid> attachmentIds,
        string? promptVersion,
        CancellationToken cancellationToken)
    {
        var summaries = new List<string>(attachmentIds.Count);
        foreach (var attachmentId in attachmentIds)
        {
            summaries.Add(await GenerateOrFallbackAsync(attachmentId, promptVersion, cancellationToken));
        }

        return summaries;
    }

    private async Task<string> GenerateOrFallbackAsync(
        Guid attachmentId,
        string? promptVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            return await GenerateAndSaveAsync(attachmentId, promptVersion, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating AI summary for attachment {AttachmentId}", attachmentId);
            return SummaryGenerationFailedMessage;
        }
    }

    public async Task<List<string>> GenerateForApplicationAsync(
        Guid applicationId,
        string? promptVersion = null,
        IReadOnlyCollection<Guid>? attachmentIds = null,
        CancellationToken cancellationToken = default)
    {
        await aiGenerationPrerequisiteValidator.EnsureAttachmentSummaryAvailableAsync(applicationId);

        var applicationAttachmentIds = (await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId))
            .Select(a => a.Id)
            .ToList();

        if (attachmentIds is not { Count: > 0 })
        {
            return await GenerateAndSaveAsync(applicationAttachmentIds, promptVersion, cancellationToken);
        }

        var applicationAttachmentIdSet = applicationAttachmentIds.ToHashSet();
        var selectedIds = attachmentIds.Distinct().ToList();

        if (selectedIds.Any(id => !applicationAttachmentIdSet.Contains(id)))
        {
            throw new InvalidOperationException("One or more selected attachments do not belong to the application.");
        }

        return await GenerateAndSaveAsync(selectedIds, promptVersion, cancellationToken);
    }

    private async Task<ChefsFileAttachmentStream> OpenAttachmentStreamAsync(
        ApplicationChefsFileAttachment attachment,
        string fileName,
        CancellationToken cancellationToken)
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
            cancellationToken.ThrowIfCancellationRequested();
            var stream = await chefsFileAttachmentStreamProvider.OpenAsync(submissionId, fileId, fileName);
            return stream ?? ChefsFileAttachmentStream.Empty;
        }
        catch (OperationCanceledException)
        {
            throw;
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
