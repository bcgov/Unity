using System;
using System.Collections.Generic;
using System.Reflection;
using Shouldly;
using Unity.AI.Generation;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications.Automation;

public class LegacyAIGenerationBackgroundJobTests(ITestOutputHelper outputHelper)
    : GrantManagerApplicationTestBase(outputHelper)
{
    public static IEnumerable<object[]> LegacyJobs =>
    [
        [typeof(GenerateApplicationAnalysisJob), typeof(GenerateApplicationAnalysisBackgroundJobArgs), AIGenerationOperations.ApplicationAnalysis, false],
        [typeof(GenerateApplicationScoringJob), typeof(GenerateApplicationScoringBackgroundJobArgs), AIGenerationOperations.ApplicationScoring, false],
        [typeof(GenerateAttachmentSummaryJob), typeof(GenerateAttachmentSummaryBackgroundJobArgs), AIGenerationOperations.AttachmentSummary, false],
        [typeof(GenerateFormMappingJob), typeof(GenerateFormMappingBackgroundJobArgs), AIGenerationOperations.FormMapping, true],
        [typeof(GenerateFormScoresheetJob), typeof(GenerateFormScoresheetBackgroundJobArgs), AIGenerationOperations.FormScoresheet, true],
        [typeof(GenerateFormWorksheetJob), typeof(GenerateFormWorksheetBackgroundJobArgs), AIGenerationOperations.FormWorksheet, true]
    ];

    [Theory]
    [MemberData(nameof(LegacyJobs))]
    public void Legacy_payload_should_map_to_generic_payload(
        Type jobType,
        Type argsType,
        string operationType,
        bool requiresFormVersion)
    {
        var applicationId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var formVersionId = Guid.NewGuid();
        var args = Activator.CreateInstance(argsType)!;

        Set(args, nameof(GenerateApplicationAnalysisBackgroundJobArgs.ApplicationId), applicationId);
        Set(args, nameof(GenerateApplicationAnalysisBackgroundJobArgs.OperationId), operationId);
        Set(args, nameof(GenerateApplicationAnalysisBackgroundJobArgs.TenantId), tenantId);
        Set(args, nameof(GenerateApplicationAnalysisBackgroundJobArgs.RequestedByUserId), userId);
        Set(args, nameof(GenerateApplicationAnalysisBackgroundJobArgs.PromptVersion), "v1");

        if (requiresFormVersion)
        {
            Set(args, nameof(GenerateFormMappingBackgroundJobArgs.ApplicationFormVersionId), formVersionId);
        }
        else if (args is GenerateAttachmentSummaryBackgroundJobArgs attachmentArgs)
        {
            attachmentArgs.AttachmentIds = [Guid.NewGuid()];
        }

        var job = Activator.CreateInstance(jobType, [null])!;
        var convert = jobType.BaseType!.GetMethod("Convert", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var mapped = (AIGenerationBackgroundJobArgs)convert.Invoke(job, [args])!;

        mapped.OperationType.ShouldBe(operationType);
        mapped.ApplicationId.ShouldBe(applicationId);
        mapped.OperationId.ShouldBe(operationId);
        mapped.TenantId.ShouldBe(tenantId);
        mapped.RequestedByUserId.ShouldBe(userId);
        mapped.PromptVersion.ShouldBe("v1");
        mapped.ApplicationFormVersionId.ShouldBe(requiresFormVersion ? formVersionId : null);
        mapped.AttachmentIds.Count.ShouldBe(args is GenerateAttachmentSummaryBackgroundJobArgs ? 1 : 0);
    }

    private static void Set(object target, string propertyName, object value) =>
        target.GetType().GetProperty(propertyName)!.SetValue(target, value);
}
