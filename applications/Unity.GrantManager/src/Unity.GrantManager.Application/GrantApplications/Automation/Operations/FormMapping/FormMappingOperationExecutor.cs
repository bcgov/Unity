using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using Unity.AI.Domain;
using Unity.AI.Generation;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.Applications;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Volo.Abp.Guids;

using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

namespace Unity.GrantManager.GrantApplications.Automation.Operations.FormMapping;

public sealed class FormMappingOperationExecutor(
    IApplicationFormVersionMappingReadService mappingReadService,
    IFormMappingService aiService,
    IGenerationReviewRepository generationReviewRepository,
    IGuidGenerator guidGenerator) : AIGenerationOperationExecutor, ITransientDependency
{
    public override string OperationType => AIGenerationOperations.FormMapping;

    protected override async Task<bool> ExecuteAsync(AIGenerationBackgroundJobArgs args)
    {
        var applicationFormVersionId = args.ApplicationFormVersionId
            ?? throw new InvalidOperationException("Form mapping generation requires an application form version.");
        var readModel = await mappingReadService.GetAsync(applicationFormVersionId);
        var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
            AIGenerationOperations.FormMapping,
            applicationFormVersionId);
        IReadOnlyCollection<string> acceptedWorksheetFields = review == null
            ? []
            : JsonSerializer.Deserialize<FormMappingReviewPayload>(review.ReviewData)
                ?.AcceptedWorksheetFields
                ?? [];
        var response = await aiService.GenerateFormMappingAsync(new FormMappingRequest
        {
            Data = FormMappingPromptDataBuilder.Build(readModel, acceptedWorksheetFields),
            PromptVersion = args.PromptVersion
        });

        if (review == null || review.Status != GenerationReviewStatus.Active)
        {
            var sequence = review?.Sequence + 1 ?? 1;
            review = new GenerationReview(
                guidGenerator.Create(),
                AIGenerationOperations.FormMapping,
                applicationFormVersionId,
                sequence);
            await generationReviewRepository.InsertAsync(review);
        }

        var suggestions = FormMappingResponseMapper.ParseSuggestions(response.Mapping)
            .Select(suggestion => new FormMappingSuggestionDto
            {
                Id = guidGenerator.Create(),
                SourceField = suggestion.SourceField,
                TargetField = suggestion.TargetField,
                Reason = suggestion.Reason,
                Confidence = suggestion.Confidence
            })
            .ToList();
        var payload = JsonSerializer.Deserialize<FormMappingReviewPayload>(review.ReviewData)
            ?? new FormMappingReviewPayload();
        payload.PendingSuggestions = suggestions;
        review.SetReviewData(JsonSerializer.Serialize(payload));
        review.SetStatus(GenerationReviewStatus.Active);
        await generationReviewRepository.UpdateAsync(review, true);

        return true;
    }
}
