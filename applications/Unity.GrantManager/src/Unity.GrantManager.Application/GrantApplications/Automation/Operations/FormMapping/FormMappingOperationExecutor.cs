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
        var response = await aiService.GenerateFormMappingAsync(new FormMappingRequest
        {
            Data = FormMappingPromptDataBuilder.Build(readModel),
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

        var rawSuggestions = FormMappingResponseMapper.ParseSuggestions(response.Mapping)
            .Select(suggestion => new FormMappingSuggestionDto
            {
                Id = guidGenerator.Create(),
                SourceField = suggestion.SourceField,
                TargetField = suggestion.TargetField,
                Reason = suggestion.Reason,
                Confidence = suggestion.Confidence
            })
            .ToList();
        var isFinalMapping = review.Sequence > 1 && review.Sequence % 2 == 0;
        var unchangedCount = 0;
        var suggestions = isFinalMapping
            ? ClassifyFinalSuggestions(readModel.ExistingMapping, rawSuggestions, out unchangedCount)
            : rawSuggestions;
        var payload = JsonSerializer.Deserialize<FormMappingReviewPayload>(review.ReviewData)
            ?? new FormMappingReviewPayload();
        payload.PendingSuggestions = suggestions;
        payload.UnchangedSuggestionCount = isFinalMapping ? unchangedCount : 0;
        payload.NoSuggestionsGenerated = suggestions.Count == 0;
        if (suggestions.Count == 0)
        {
            review.Complete();
        }
        review.SetReviewData(JsonSerializer.Serialize(payload));
        if (suggestions.Count > 0)
        {
            review.SetStatus(GenerationReviewStatus.Active);
        }
        await generationReviewRepository.UpdateAsync(review, true);

        return true;
    }

    internal static List<FormMappingSuggestionDto> ClassifyFinalSuggestions(
        string? existingMapping,
        List<FormMappingSuggestionDto> suggestions,
        out int unchangedCount)
    {
        var existing = ParseMapping(existingMapping);
        var bySource = existing.GroupBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Key, StringComparer.OrdinalIgnoreCase);
        var byTarget = existing.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        unchangedCount = 0;
        var actionable = new List<FormMappingSuggestionDto>();

        foreach (var suggestion in suggestions)
        {
            if (bySource.TryGetValue(suggestion.SourceField, out var previousTarget) &&
                previousTarget.Equals(suggestion.TargetField, StringComparison.OrdinalIgnoreCase))
            {
                unchangedCount++;
                continue;
            }

            suggestion.ChangeType = bySource.ContainsKey(suggestion.SourceField) ? "Changed" : "New";
            suggestion.PreviousTargetField = bySource.GetValueOrDefault(suggestion.SourceField);
            suggestion.ConflictSourceField = byTarget.TryGetValue(suggestion.TargetField, out var conflictSource) &&
                !conflictSource.Equals(suggestion.SourceField, StringComparison.OrdinalIgnoreCase)
                ? conflictSource
                : null;
            actionable.Add(suggestion);
        }

        return actionable;
    }

    private static Dictionary<string, string> ParseMapping(string? mapping)
    {
        if (string.IsNullOrWhiteSpace(mapping))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(mapping)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
