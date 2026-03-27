using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System;
using System.Threading.Tasks;
using System.Globalization;
using Unity.GrantManager.Assessments;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets;
using System.Linq;
using Unity.Flex.Worksheets;
using Unity.Flex;
using Unity.Flex.Scoresheets.Enums;
using Unity.GrantManager.AI.Models;
using Unity.GrantManager.Applications;
using System.Text.Json;
using Unity.AI.Permissions;
using Unity.AI.Settings;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;
using Volo.Abp.Settings;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentScoresWidget
{
    [Widget(
        RefreshUrl = "Widgets/AssessmentScores/RefreshAssessmentScores",
        ScriptTypes = new[] { typeof(AssessmentScoresWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(AssessmentScoresWidgetStyleBundleContributor) },
        AutoInitialize = true)]
    public class AssessmentScoresWidgetViewComponent(IAssessmentRepository assessmentRepository,
        IScoresheetRepository scoresheetRepository,
        IScoresheetInstanceRepository scoresheetInstanceRepository,
        IApplicationRepository applicationRepository,
        IFeatureChecker featureChecker,
        IPermissionChecker permissionChecker,
        ISettingProvider settingProvider) : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(Guid assessmentId, Guid currentUserId)
        {
            if (assessmentId == Guid.Empty)
            {
                return View(new AssessmentScoresWidgetViewModel());
            }

            var assessment = await assessmentRepository.GetAsync(assessmentId);
            var application = await applicationRepository.GetAsync(assessment.ApplicationId);
            var scoresheetInstance = await scoresheetInstanceRepository.GetByCorrelationAsync(assessment.Id);

            // Parse AI scoresheet answers if available
            Dictionary<string, JsonElement>? aiAnswers = null;
            if (!string.IsNullOrEmpty(application.AIScoresheetAnswers))
            {
                try
                {
                    var aiAnswersJson = JsonDocument.Parse(application.AIScoresheetAnswers);
                    aiAnswers = [];
                    foreach (var property in aiAnswersJson.RootElement.EnumerateObject())
                    {
                        aiAnswers[property.Name] = property.Value;
                    }
                }
                catch (JsonException)
                {
                    // If AI answers are malformed, continue without them
                    aiAnswers = null;
                }
            }

            ScoresheetDto? scoresheetDto = null;
            if (scoresheetInstance != null)
            {
                var scoresheet = await scoresheetRepository.GetWithChildrenAsync(scoresheetInstance.ScoresheetId);
                if (scoresheet != null)
                {
                    scoresheetDto = ObjectMapper.Map<Scoresheet, ScoresheetDto?>(scoresheet);

                    // Create a set to track which questions have human answers
                    var humanAnsweredQuestions = new HashSet<Guid>();

                    // First, populate human answers
                    ResolveAnswers(scoresheetInstance, scoresheetDto, humanAnsweredQuestions);

                    // Only show AI suggestions on the dedicated AI assessment row.
                    // Human assessments (including clones) show only their own saved answers.
                    if (assessment.IsAiAssessment)
                    {
                        ResolveAiAnswers(aiAnswers, scoresheetDto, humanAnsweredQuestions);
                    }
                }
            }
            AssessmentScoresWidgetViewModel model = new()
            {
                AssessmentId = assessmentId,
                Scoresheet = scoresheetDto,
                FinancialAnalysis = assessment.FinancialAnalysis,
                EconomicImpact = assessment.EconomicImpact,
                InclusiveGrowth = assessment.InclusiveGrowth,
                CleanGrowth = assessment.CleanGrowth,
                Status = assessment.Status,
                CurrentUserId = currentUserId,
                AssessorId = assessment.AssessorId,
                IsAIScoringEnabled = await featureChecker.IsEnabledAsync("Unity.AI.Scoring") &&
                    await permissionChecker.IsGrantedAsync(AIPermissions.ScoringAssistant.ScoringAssistantDefault) &&
                    await settingProvider.GetAsync<bool>(AISettings.ScoringAssistantEnabled, defaultValue: false),
                IsAiAssessment = assessment.IsAiAssessment,
            };

            return View(model);
        }

        private static void ResolveAiAnswers(Dictionary<string, JsonElement>? aiAnswers, ScoresheetDto? scoresheetDto, HashSet<Guid> humanAnsweredQuestions)
        {
            if (aiAnswers != null && scoresheetDto != null)
            {
                foreach (var section in scoresheetDto.Sections)
                {
                    foreach (var question in section.Fields.Where(q => !humanAnsweredQuestions.Contains(q.Id)))
                    {
                        ResolveAiAnswer(aiAnswers, question);
                    }
                }
            }
        }

        private static void ResolveAiAnswer(Dictionary<string, JsonElement> aiAnswers, QuestionDto question)
        {
            if (aiAnswers.TryGetValue(question.Id.ToString(), out var aiAnswerValue))
            {
                question.IsHumanConfirmed = false; // Mark as AI generated

                // Handle AI response format with answer, rationale, and confidence.
                if (aiAnswerValue.ValueKind == JsonValueKind.Object)
                {
                    // New format with rationale and confidence
                    if (aiAnswerValue.TryGetProperty(AIJsonKeys.Answer, out var answerProp))
                    {
                        var rawAnswer = answerProp.ToString();

                        // For select list questions, convert numeric answer to actual option value
                        if (question.Type == QuestionType.SelectList)
                        {
                            question.Answer = ConvertNumericAnswerToSelectListValue(rawAnswer, question.Definition);
                        }
                        else
                        {
                            question.Answer = rawAnswer;
                        }
                    }
                    if (aiAnswerValue.TryGetProperty(AIJsonKeys.Rationale, out var rationaleProp))
                    {
                        question.AICitation = rationaleProp.ToString();
                    }
                    if (aiAnswerValue.TryGetProperty(AIJsonKeys.Confidence, out var confidenceProp))
                    {
                        question.AIConfidence = ParseAiConfidence(confidenceProp);
                    }
                }
                else
                {
                    // Fallback for simple string format (backward compatibility)
                    var rawAnswer = aiAnswerValue.ToString();
                    if (question.Type == QuestionType.SelectList)
                    {
                        question.Answer = ConvertNumericAnswerToSelectListValue(rawAnswer, question.Definition);
                    }
                    else
                    {
                        question.Answer = rawAnswer;
                    }
                }
            }
        }

        private static void ResolveAnswers(ScoresheetInstance scoresheetInstance, ScoresheetDto? scoresheetDto, HashSet<Guid> humanAnsweredQuestions)
        {
            foreach (var answer in scoresheetInstance.Answers)
            {
                humanAnsweredQuestions.Add(answer.QuestionId);

                if (scoresheetDto != null)
                {
                    foreach (var section in scoresheetDto.Sections)
                    {
                        var question = section.Fields.FirstOrDefault(q => q.Id == answer.QuestionId);
                        if (question != null)
                        {
                            question.IsHumanConfirmed = true; // Mark as human confirmed
                            switch (question.Type)
                            {
                                case QuestionType.Number:
                                    {
                                        question.Answer = ValueResolver.Resolve(answer.CurrentValue!, CustomFieldType.Numeric)!.ToString();
                                        break;
                                    }
                                case QuestionType.YesNo:
                                    {
                                        question.Answer = ValueResolver.Resolve(answer.CurrentValue!, CustomFieldType.YesNo)!.ToString();
                                        break;
                                    }
                                case QuestionType.Text:
                                    {
                                        question.Answer = ValueResolver.Resolve(answer.CurrentValue!, CustomFieldType.Text)!.ToString();
                                        break;
                                    }
                                case QuestionType.SelectList:
                                    {
                                        question.Answer = ValueResolver.Resolve(answer.CurrentValue!, CustomFieldType.SelectList)!.ToString();
                                        break;
                                    }
                                case QuestionType.TextArea:
                                    {
                                        question.Answer = ValueResolver.Resolve(answer.CurrentValue!, CustomFieldType.TextArea)!.ToString();
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
        }

        private static string ConvertNumericAnswerToSelectListValue(string numericAnswer, string? definition)
        {
            if (string.IsNullOrEmpty(definition) || string.IsNullOrEmpty(numericAnswer))
                return numericAnswer;

            try
            {
                // Parse the numeric answer
                if (!int.TryParse(numericAnswer.Trim(), out var optionNumber) || optionNumber <= 0)
                    return numericAnswer;

                // Parse the select list definition
                var selectListDefinition = JsonSerializer.Deserialize<Unity.Flex.Worksheets.Definitions.QuestionSelectListDefinition>(definition);

                if (selectListDefinition?.Options != null && selectListDefinition.Options.Count > 0)
                {
                    // Convert 1-based index to 0-based
                    var optionIndex = optionNumber - 1;

                    if (optionIndex < selectListDefinition.Options.Count)
                    {
                        return selectListDefinition.Options[optionIndex].Value;
                    }
                }
            }
            catch (JsonException)
            {
                // If parsing fails, return original answer
            }

            return numericAnswer;
        }

        private static int ParseAiConfidence(JsonElement confidenceProp)
        {
            int confidence = 0;

            if (confidenceProp.ValueKind == JsonValueKind.Number)
            {
                if (confidenceProp.TryGetInt32(out var intValue))
                {
                    confidence = intValue;
                }
                else if (confidenceProp.TryGetDouble(out var doubleValue))
                {
                    confidence = (int)Math.Round(doubleValue, MidpointRounding.AwayFromZero);
                }
            }
            else if (confidenceProp.ValueKind == JsonValueKind.String)
            {
                var raw = confidenceProp.GetString();
                if (!int.TryParse(raw, out confidence) &&
                    double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDouble))
                {
                    confidence = (int)Math.Round(parsedDouble, MidpointRounding.AwayFromZero);
                }
            }

            var rounded = (int)Math.Round(confidence / 5.0, MidpointRounding.AwayFromZero) * 5;
            return Math.Clamp(rounded, 0, 100);
        }

    }

    public class AssessmentScoresWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/AssessmentScoresWidget/Default.css");
        }
    }

    public class AssessmentScoresWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/AssessmentScoresWidget/Default.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
