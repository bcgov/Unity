using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Generation;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.Applications;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

namespace Unity.GrantManager.GrantApplications.Automation.Operations.FormMapping;

public sealed class FormMappingOperationExecutor(
    IApplicationFormVersionMappingReadService mappingReadService,
    IFormMappingService aiService,
    IRepository<ApplicationFormVersion, Guid> applicationFormVersionRepository) : AIGenerationOperationExecutor, ITransientDependency
{
    public override string OperationType => AIGenerationOperations.FormMapping;

    protected override async Task<bool> ExecuteAsync(AIGenerationBackgroundJobArgs args)
    {
        var applicationFormVersionId = args.ApplicationFormVersionId
            ?? throw new InvalidOperationException("Form mapping generation requires an application form version.");
        var readModel = await mappingReadService.GetAsync(applicationFormVersionId);
        var response = await aiService.GenerateFormMappingAsync(new FormMappingRequest
        {
            Data = FormMappingPromptDataBuilder.Build(readModel),
            PromptVersion = args.PromptVersion
        });

        var submissionHeaderMapping = FormMappingResponseMapper.BuildSubmissionHeaderMapping(response);
        var applicationFormVersion = await applicationFormVersionRepository.GetAsync(applicationFormVersionId);
        applicationFormVersion.SubmissionHeaderMapping = submissionHeaderMapping;
        await applicationFormVersionRepository.UpdateAsync(applicationFormVersion, true);

        return true;
    }
}
