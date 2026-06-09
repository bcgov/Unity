using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ApplicationScoringService(
        IAIService aiService,
        AIExecutionModeResolver executionModeResolver,
        ILogger<ApplicationScoringService> logger) : IApplicationScoringService, ITransientDependency
    {
        public async Task<string> RegenerateAsync(ApplicationScoringOperationInputDto input, CancellationToken cancellationToken = default)
        {
            var sections = input.Sections;
            var mode = executionModeResolver.ResolveMode(AIExecutionModeResolver.ApplicationScoringOperation);

            var perSectionResults = await AIExecutionStrategy.RunAsync(
                sections,
                mode,
                section => ProcessSectionAsync(input.ApplicationId, section, input.Data, input.Attachments, input.PromptVersion, cancellationToken),
                batch => ProcessSectionsAsync(input.ApplicationId, batch, input.Data, input.Attachments, input.PromptVersion, cancellationToken));

            var allSectionResults = new Dictionary<string, object>();
            foreach (var sectionResult in perSectionResults)
            {
                foreach (var kvp in sectionResult)
                {
                    allSectionResults[kvp.Key] = kvp.Value;
                }
            }

            var combinedResults = JsonSerializer.Serialize(allSectionResults, AIJsonDefaults.Indented);
            var validatedJson = ValidateApplicationScoringJson(combinedResults);
            return validatedJson;
        }

        private async Task<Dictionary<string, object>> ProcessSectionAsync(
            Guid applicationId,
            ApplicationScoringSectionOperationInputDto section,
            JsonElement promptData,
            List<AIAttachmentItem> attachmentSummaries,
            string? promptVersion,
            CancellationToken cancellationToken)
        {
            var sectionResults = new Dictionary<string, object>();
            try
            {
                var applicationScoringRequest = new ApplicationScoringRequest
                {
                    Data = promptData,
                    Attachments = attachmentSummaries,
                    SectionName = section.SectionName,
                    SectionSchema = section.SectionSchema,
                    PromptVersion = promptVersion,
                };
                var applicationScoringResponse = await aiService.GenerateApplicationScoringAsync(applicationScoringRequest, cancellationToken);

                if (applicationScoringResponse.Answers.Count > 0)
                {
                    CopyAnswers(applicationScoringResponse.Answers, sectionResults);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing AI application scoring section {SectionName} for application {ApplicationId}", section.SectionName, applicationId);
            }

            return sectionResults;
        }

        private async Task<List<Dictionary<string, object>>> ProcessSectionsAsync(
            Guid applicationId,
            IReadOnlyCollection<ApplicationScoringSectionOperationInputDto> sections,
            JsonElement promptData,
            List<AIAttachmentItem> attachmentSummaries,
            string? promptVersion,
            CancellationToken cancellationToken)
        {
            var sectionResults = new Dictionary<string, object>();
            try
            {
                var questions = BuildBatchSectionSchema(sections);

                var applicationScoringRequest = new ApplicationScoringRequest
                {
                    Data = promptData,
                    Attachments = attachmentSummaries,
                    SectionName = "All Sections",
                    SectionSchema = questions,
                    PromptVersion = promptVersion,
                };
                var applicationScoringResponse = await aiService.GenerateApplicationScoringAsync(applicationScoringRequest, cancellationToken);

                if (applicationScoringResponse.Answers.Count > 0)
                {
                    CopyAnswers(applicationScoringResponse.Answers, sectionResults);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing AI application scoring batch for application {ApplicationId}", applicationId);
            }

            return [sectionResults];
        }

        private static JsonElement BuildBatchSectionSchema(IReadOnlyCollection<ApplicationScoringSectionOperationInputDto> sections)
        {
            foreach (var section in sections)
            {
                if (section.SectionSchema.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException(
                        $"Section schema for '{section.SectionName}' must be a JSON array.");
                }
            }

            var questions = new List<JsonElement>();
            foreach (var section in sections)
            {
                foreach (var question in section.SectionSchema.EnumerateArray())
                {
                    questions.Add(question.Clone());
                }
            }

            return JsonSerializer.SerializeToElement(questions, AIJsonDefaults.IndentedCamelCase);
        }

        private void CopyAnswers(Dictionary<string, ApplicationScoringAnswer> answers, Dictionary<string, object> results)
        {
            var answersJson = JsonSerializer.Serialize(answers, AIJsonDefaults.IndentedCamelCase);
            using var answersDoc = JsonDocument.Parse(answersJson);
            foreach (var property in answersDoc.RootElement.EnumerateObject())
            {
                results[property.Name] = property.Value.Clone();
            }
        }

        private static string ValidateApplicationScoringJson(string scoresheetAnswers)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(scoresheetAnswers))
                {
                    using var _ = JsonDocument.Parse(scoresheetAnswers);
                    return scoresheetAnswers;
                }
            }
            catch (JsonException)
            {
                // Fall back to empty object for invalid JSON.
            }

            return "{}";
        }
    }
}
