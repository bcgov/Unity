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
    IFormMappingReviewRepository mappingReviewRepository,
    IGuidGenerator guidGenerator) : AIGenerationOperationExecutor, ITransientDependency
{
    public override string OperationType => AIGenerationOperations.FormMapping;

    protected override async Task<bool> ExecuteAsync(AIGenerationBackgroundJobArgs args)
    {
        var applicationFormVersionId = args.ApplicationFormVersionId
            ?? throw new InvalidOperationException("Form mapping generation requires an application form version.");
        var readModel = await mappingReadService.GetAsync(applicationFormVersionId);
        var review = await mappingReviewRepository.FindByFormVersionAsync(applicationFormVersionId);
        var acceptedWorksheetFields = review == null
            ? []
            : JsonSerializer.Deserialize<List<string>>(review.AcceptedWorksheetFieldsJson) ?? [];
        var response = await aiService.GenerateFormMappingAsync(new FormMappingRequest
        {
            Data = FormMappingPromptDataBuilder.Build(readModel, acceptedWorksheetFields),
            PromptVersion = args.PromptVersion
        });

        if (review == null)
        {
            review = new FormMappingReview(guidGenerator.Create(), applicationFormVersionId);
            await mappingReviewRepository.InsertAsync(review);
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
        review.SetPendingMappingSuggestions(JsonSerializer.Serialize(suggestions));
        if (review.Phase == FormMappingReviewPhase.Completed)
        {
            review.SetPhase(FormMappingReviewPhase.FinalMappingReview);
        }

        await mappingReviewRepository.UpdateAsync(review, true);

        return true;
    }
}
