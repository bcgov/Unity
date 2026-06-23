using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Models;
using Unity.AI.Requests;
using Unity.AI.Runtime;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Operations
{
    public class ApplicationAnalysisService(
        IAIService aiService,
        IAIGenerationPrerequisiteValidator aiGenerationPrerequisiteValidator) : IApplicationAnalysisService, ITransientDependency
    {
        public async Task<string> RegenerateAsync(ApplicationAnalysisOperationInputDto input, CancellationToken cancellationToken = default)
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
            return analysisJson;
        }

    }
}
