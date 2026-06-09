using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Models;
using Unity.AI.Requests;
using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Operations
{
    public class ApplicationAnalysisService(
        IApplicationRepository applicationRepository,
        IAIService aiService,
        IAIGenerationPrerequisiteValidator aiGenerationPrerequisiteValidator) : IApplicationAnalysisService, ITransientDependency
    {
        public async Task<string> RegenerateAndSaveAsync(ApplicationAnalysisOperationInputDto input, CancellationToken cancellationToken = default)
        {
            await aiGenerationPrerequisiteValidator.EnsureApplicationAnalysisAvailableAsync(input.ApplicationId);

            var analysis = await aiService.GenerateApplicationAnalysisAsync(new ApplicationAnalysisRequest
            {
                Schema = input.Schema,
                Data = input.Data,
                Attachments = input.Attachments,
                PromptVersion = input.PromptVersion,
            }, cancellationToken);

            var analysisJson = JsonSerializer.Serialize(analysis, AIJsonDefaults.Indented);
            var application = await applicationRepository.GetAsync(input.ApplicationId);
            application.AIAnalysis = analysisJson;
            await applicationRepository.UpdateAsync(application);
            return analysisJson;
        }

    }
}
