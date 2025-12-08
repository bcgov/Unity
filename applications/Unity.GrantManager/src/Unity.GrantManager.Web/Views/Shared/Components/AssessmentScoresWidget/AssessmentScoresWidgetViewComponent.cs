using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Assessments;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets;
using System.Linq;
using Unity.Flex.Worksheets;
using Unity.Flex;
using Unity.Flex.Scoresheets.Enums;
using Unity.GrantManager.Applications;
using System.Text.Json;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentScoresWidget
{
    [Widget(
        RefreshUrl = "Widgets/AssessmentScores/RefreshAssessmentScores",
        ScriptTypes = new[] { typeof(AssessmentScoresWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(AssessmentScoresWidgetStyleBundleContributor) },
        AutoInitialize = true)]
    public class AssessmentScoresWidgetViewComponent : AbpViewComponent
    {
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IScoresheetRepository _scoresheetRepository;
        private readonly IScoresheetInstanceRepository _scoresheetInstanceRepository;
        private readonly IApplicationRepository _applicationRepository;
        public AssessmentScoresWidgetViewComponent(IAssessmentRepository assessmentRepository, IScoresheetRepository scoresheetRepository, IScoresheetInstanceRepository scoresheetInstanceRepository, IApplicationRepository applicationRepository)
        {
            _assessmentRepository = assessmentRepository;
            _scoresheetRepository = scoresheetRepository;
            _scoresheetInstanceRepository = scoresheetInstanceRepository;
            _applicationRepository = applicationRepository;
        }


        public async Task<IViewComponentResult> InvokeAsync(Guid assessmentId, Guid currentUserId)
        {
            if (assessmentId == Guid.Empty)
            {
                return View(new AssessmentScoresWidgetViewModel());
            }
            var assessment = await _assessmentRepository.GetAsync(assessmentId);
            var application = await _applicationRepository.GetAsync(assessment.ApplicationId);
            var scoresheetInstance = await _scoresheetInstanceRepository.GetByCorrelationAsync(assessment.Id);
            
            // Parse AI scoresheet answers if available
            Dictionary<string, JsonElement>? aiAnswers = null;
            if (!string.IsNullOrEmpty(application.AIScoresheetAnswers))
            {
                try
                {
                    var aiAnswersJson = JsonDocument.Parse(application.AIScoresheetAnswers);
                    aiAnswers = new Dictionary<string, JsonElement>();
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
                var scoresheet = await _scoresheetRepository.GetWithChildrenAsync(scoresheetInstance.ScoresheetId);
                if (scoresheet != null)
                {
                    scoresheetDto = ObjectMapper.Map<Scoresheet, ScoresheetDto?>(scoresheet);

                    // Create a set to track which questions have human answers
                    var humanAnsweredQuestions = new HashSet<Guid>();
                    
                    // First, populate human answers
                    foreach (var answer in scoresheetInstance.Answers)
                    {
                        humanAnsweredQuestions.Add(answer.QuestionId);
                        foreach (var section in scoresheetDto!.Sections)
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
                    // Then, populate AI answers for questions without human answers
                    if (aiAnswers != null)
                    {
                        foreach (var section in scoresheetDto.Sections)
                        {
                            foreach (var question in section.Fields.Where(q => !humanAnsweredQuestions.Contains(q.Id)))
                            {
                                if (aiAnswers.TryGetValue(question.Id.ToString(), out var aiAnswerValue))
                                {
                                    question.IsHumanConfirmed = false; // Mark as AI generated
                                    
                                    // Handle enhanced AI response format with answer, citation, and confidence
                                    if (aiAnswerValue.ValueKind == JsonValueKind.Object)
                                    {
                                        // New format with citations and confidence scores
                                        if (aiAnswerValue.TryGetProperty("answer", out var answerProp))
                                        {
                                            var rawAnswer = answerProp.ToString();
                                            
                                            // For select list questions, convert numeric answer to actual option value
                                            if (question.Type == Unity.Flex.Scoresheets.Enums.QuestionType.SelectList)
                                            {
                                                question.Answer = ConvertNumericAnswerToSelectListValue(rawAnswer, question.Definition);
                                            }
                                            else
                                            {
                                                question.Answer = rawAnswer;
                                            }
                                        }
                                        if (aiAnswerValue.TryGetProperty("citation", out var citationProp))
                                        {
                                            question.AICitation = citationProp.ToString();
                                        }
                                        if (aiAnswerValue.TryGetProperty("confidence", out var confidenceProp) && 
                                            confidenceProp.TryGetInt32(out var confidenceScore))
                                        {
                                            question.AIConfidence = confidenceScore;
                                        }
                                    }
                                    else
                                    {
                                        // Fallback for simple string format (backward compatibility)
                                        var rawAnswer = aiAnswerValue.ToString();
                                        if (question.Type == Unity.Flex.Scoresheets.Enums.QuestionType.SelectList)
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
                        }
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
            };

            return View(model);
        }

        private string ConvertNumericAnswerToSelectListValue(string numericAnswer, string? definition)
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
                if (selectListDefinition?.Options != null && selectListDefinition.Options.Any())
                {
                    // Convert 1-based index to 0-based
                    var optionIndex = optionNumber - 1;
                    if (optionIndex >= 0 && optionIndex < selectListDefinition.Options.Count)
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
