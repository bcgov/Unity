using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Extraction;
using Unity.AI.Localization;
using Unity.AI.Requests;
using Unity.GrantManager.Intakes;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace Unity.AI.Operations;

public class AttachmentSummaryService(
    IAttachmentSummaryPersistence attachmentSummaryPersistence,
    IChefsFileAttachmentStreamProvider chefsFileAttachmentStreamProvider,
    ITextExtractionService textExtractionService,
    IAIService aiService,
    IAIGenerationPrerequisiteValidator aiGenerationPrerequisiteValidator,
    AIExecutionModeResolver executionModeResolver,
    IUnitOfWorkManager unitOfWorkManager,
    ILogger<AttachmentSummaryService> logger,
    IStringLocalizer<AIResource> localizer) : IAttachmentSummaryService, ITransientDependency
{
    private const string SummaryGenerationFailedMessage = "AI summary generation failed.";
    private const string TextExtractionFailedSummary = "Attachment text could not be extracted for AI summary generation.";

    public async Task<string> GenerateAndSaveAsync(Guid attachmentId, string? promptVersion = null, CancellationToken cancellationToken = default)
    {
        var attachment = await attachmentSummaryPersistence.LoadAsync(attachmentId);
        var fileName = string.IsNullOrWhiteSpace(attachment.FileName) ? "unknown" : attachment.FileName;

        await using var attachmentStream = await OpenAttachmentStreamAsync(attachment, fileName, cancellationToken);
        var extractedText = await textExtractionService.ExtractTextAsync(fileName, attachmentStream.Content, attachmentStream.ContentType, cancellationToken);
        if (ShouldStopOnEmptyExtraction(fileName, extractedText))
        {
            LogEmptyExtraction(attachmentId, fileName, attachmentStream);
        await attachmentSummaryPersistence.SaveSummaryAsync(attachmentId, TextExtractionFailedSummary);
        return TextExtractionFailedSummary;
        }

        var summaryResponse = await aiService.GenerateAttachmentSummaryAsync(new AttachmentSummaryRequest
        {
            FileName = fileName,
            ContentType = attachmentStream.ContentType,
            ExtractedText = extractedText,
            PromptVersion = promptVersion,
        }, cancellationToken);

        await attachmentSummaryPersistence.SaveSummaryAsync(attachmentId, summaryResponse.Summary);

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
        if (mode == AIExecutionMode.Batch)
        {
            return await GenerateBatchAsync(ids, promptVersion, cancellationToken);
        }

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

    private async Task<List<string>> GenerateBatchAsync(
        IReadOnlyCollection<Guid> attachmentIds,
        string? promptVersion,
        CancellationToken cancellationToken)
    {
        var attachments = new List<(Guid Id, AttachmentSummarySource Source, string ContentType, string ExtractedText)>(attachmentIds.Count);
        var failures = new Dictionary<Guid, string>();

        foreach (var attachmentId in attachmentIds)
        {
            try
            {
                var attachment = await LoadAttachmentAsync(attachmentId);
                var fileName = string.IsNullOrWhiteSpace(attachment.FileName) ? "unknown" : attachment.FileName;
                await using var attachmentStream = await OpenAttachmentStreamAsync(attachment, fileName, cancellationToken);
                var extractedText = await textExtractionService.ExtractTextAsync(fileName, attachmentStream.Content, attachmentStream.ContentType, cancellationToken);
                if (ShouldStopOnEmptyExtraction(fileName, extractedText))
                {
                    LogEmptyExtraction(attachmentId, fileName, attachmentStream);
                    failures[attachmentId] = TextExtractionFailedSummary;
                    continue;
                }

                attachments.Add((attachmentId, attachment, attachmentStream.ContentType, extractedText));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error preparing AI summary batch item {AttachmentId}", attachmentId);
                failures[attachmentId] = SummaryGenerationFailedMessage;
            }
        }

        if (attachments.Count == 0)
        {
            return attachmentIds.Select(id => failures.TryGetValue(id, out var failure) ? failure : SummaryGenerationFailedMessage).ToList();
        }

        var batchRequest = new AttachmentSummaryBatchRequest
        {
            PromptVersion = promptVersion,
            Attachments = attachments.Select(item => new AttachmentSummaryBatchItemRequest
            {
                AttachmentId = item.Id.ToString(),
                FileName = string.IsNullOrWhiteSpace(item.Source.FileName) ? "unknown" : item.Source.FileName!,
                ContentType = item.ContentType,
                ExtractedText = item.ExtractedText
            }).ToList()
        };

        var batchResponse = await aiService.GenerateAttachmentSummaryBatchAsync(batchRequest, cancellationToken);
        var responseMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in batchResponse.Attachments)
        {
            if (!string.IsNullOrWhiteSpace(item.AttachmentId))
            {
                responseMap[item.AttachmentId] = item.Summary;
            }
        }

        var results = new List<string>(attachmentIds.Count);
        foreach (var attachmentId in attachmentIds)
        {
            if (failures.TryGetValue(attachmentId, out var failure))
            {
                results.Add(failure);
                continue;
            }

            if (responseMap.TryGetValue(attachmentId.ToString(), out var summary))
            {
                await SaveSummaryAsync(attachmentId, summary);
                results.Add(summary);
                continue;
            }

            results.Add(SummaryGenerationFailedMessage);
        }

        return results;
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
        await WithUnitOfWorkAsync(() => aiGenerationPrerequisiteValidator.EnsureAttachmentSummaryAvailableAsync(applicationId));

        var applicationAttachmentIds = await attachmentSummaryPersistence.LoadApplicationAttachmentIdsAsync(applicationId);

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

    private async Task WithUnitOfWorkAsync(Func<Task> operation)
    {
        using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        await operation();
        await uow.CompleteAsync();
    }

    private async Task<ChefsFileAttachmentStream> OpenAttachmentStreamAsync(
        AttachmentSummarySource attachment,
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

    private static bool ShouldStopOnEmptyExtraction(string fileName, string extractedText)
    {
        return string.IsNullOrWhiteSpace(extractedText) && IsSupportedOfficeOrPdf(fileName);
    }

    private static bool IsSupportedOfficeOrPdf(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return extension is ".pdf" or ".docx" or ".xlsx" or ".xls" or ".pptx";
    }

    private void LogEmptyExtraction(
        Guid attachmentId,
        string fileName,
        ChefsFileAttachmentStream attachmentStream)
    {
        logger.LogWarning(
            "No text extracted for supported attachment {AttachmentId} ({FileName}). Skipping AI summary generation. ContentType: {ContentType}; StreamCanSeek: {StreamCanSeek}; StreamLength: {StreamLength}.",
            attachmentId,
            fileName,
            attachmentStream.ContentType,
            attachmentStream.Content.CanSeek,
            TryGetStreamLength(attachmentStream.Content));
    }

    private static long? TryGetStreamLength(Stream stream)
    {
        if (!stream.CanSeek)
        {
            return null;
        }

        try
        {
            return stream.Length;
        }
        catch
        {
            return null;
        }
    }

}
