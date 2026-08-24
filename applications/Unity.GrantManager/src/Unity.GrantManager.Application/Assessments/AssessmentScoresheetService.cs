using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex;
using Unity.Flex.Scoresheets;
using Unity.Flex.Scoresheets.Enums;
using Unity.Flex.Scoresheets.Events;
using Unity.Flex.Worksheets.Definitions;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Exceptions;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Assessments
{
    /// <summary>
    /// Coordinates the scoresheet-related concerns of an Assessment: subtotal calculation,
    /// checking whether an application's form has a scoresheet linked, validating scoresheet
    /// answers on workflow completion, cloning AI-generated answers onto a new human assessment,
    /// and persisting scoresheet section answers. Extracted out of <see cref="AssessmentAppService"/>
    /// so that class's constructor stays focused on assessment-level collaborators.
    /// </summary>
    public class AssessmentScoresheetService : IAssessmentScoresheetService, ITransientDependency
    {
        private readonly IFeatureChecker _featureChecker;
        private readonly IScoresheetInstanceAppService _scoresheetInstanceAppService;
        private readonly IScoresheetAppService _scoresheetAppService;
        private readonly IRepository<ApplicationScoresheetAnswers, Guid> _scoresheetAnswersRepository;
        private readonly IRepository<ApplicationForm, Guid> _applicationFormRepository;
        private readonly ILocalEventBus _localEventBus;
        private const string UnityFlex = "Unity.Flex";

        public AssessmentScoresheetService(
            IFeatureChecker featureChecker,
            IScoresheetInstanceAppService scoresheetInstanceAppService,
            IScoresheetAppService scoresheetAppService,
            IRepository<ApplicationScoresheetAnswers, Guid> scoresheetAnswersRepository,
            IRepository<ApplicationForm, Guid> applicationFormRepository,
            ILocalEventBus localEventBus)
        {
            _featureChecker = featureChecker;
            _scoresheetInstanceAppService = scoresheetInstanceAppService;
            _scoresheetAppService = scoresheetAppService;
            _scoresheetAnswersRepository = scoresheetAnswersRepository;
            _applicationFormRepository = applicationFormRepository;
            _localEventBus = localEventBus;
        }

        public async Task<SubTotalDto> GetSubTotalAsync(AssessmentListItemDto assessment)
        {
            if (await _featureChecker.IsEnabledAsync(UnityFlex))
            {
                var instance = await _scoresheetInstanceAppService.GetByCorrelationAsync(assessment.Id);

                if (instance == null)
                {

                    double subTotal = (assessment.FinancialAnalysis ?? 0) + (assessment.EconomicImpact ?? 0) + (assessment.InclusiveGrowth ?? 0) + (assessment.CleanGrowth ?? 0);
                    return new SubTotalDto { SubTotal = subTotal, IsUsingDefaultScoresheet = true };

                }
                else
                {
                    var questionIds = instance.Answers.Select(a => a.QuestionId).Distinct().ToList();

                    var numericSubtotal = await GetNumericAnswerSubtotal(instance, questionIds);
                    var yesNoSubtotal = await GetYesNoAnswerSubtotal(instance, questionIds);
                    var selectListSubtotal = await GetSelectListAnswerSubtotal(instance, questionIds);

                    double subTotal = numericSubtotal + yesNoSubtotal + selectListSubtotal;
                    return new SubTotalDto { SubTotal = subTotal, IsUsingDefaultScoresheet = false };

                }
            }
            else
            {
                double subTotal = (assessment.FinancialAnalysis ?? 0) + (assessment.EconomicImpact ?? 0) + (assessment.InclusiveGrowth ?? 0) + (assessment.CleanGrowth ?? 0);
                return new SubTotalDto { SubTotal = subTotal, IsUsingDefaultScoresheet = true };
            }
        }

        public async Task<bool> IsScoresheetNotLinkedToFormAsync(Guid applicationFormId)
        {
            var applicationForm = await _applicationFormRepository.GetAsync(applicationFormId);
            return applicationForm.ScoresheetId == null;
        }

        public async Task ValidateAnswersOnCompleteAsync(Guid assessmentId, AssessmentAction triggerAction)
        {
            if (await _featureChecker.IsEnabledAsync(UnityFlex) && triggerAction == AssessmentAction.Complete)
            {
                var requirementsMetResult = await _scoresheetInstanceAppService.ValidateAnswersAsync(assessmentId);

                if (requirementsMetResult?.Errors?.Count > 0)
                {
                    throw new InvalidScoresheetAnswersException([.. requirementsMetResult.Errors]);
                }
            }
        }

        /// <summary>
        /// If the Unity.Flex feature is enabled and stored AI scoresheet answers exist for the
        /// given application, copies them onto the given human assessment's scoresheet instance.
        /// </summary>
        public async Task CopyAiAnswersIfEnabledAsync(Guid applicationId, Guid newAssessmentId)
        {
            if (!await _featureChecker.IsEnabledAsync(UnityFlex))
            {
                return;
            }

            var storedAiAnswers = await _scoresheetAnswersRepository
                .FindAsync(x => x.ApplicationId == applicationId);

            if (string.IsNullOrEmpty(storedAiAnswers?.Answers))
            {
                return;
            }

            await CopyAiAnswersToAssessmentAsync(storedAiAnswers.Answers, newAssessmentId);
        }

        /// <summary>
        /// If the Unity.Flex feature is enabled, persists the given scoresheet section answers
        /// on the given assessment by publishing a <see cref="PersistScoresheetSectionInstanceEto"/>
        /// local event.
        /// </summary>
        public async Task PersistSectionAnswersIfEnabledAsync(Guid assessmentId, List<AssessmentAnswersEto> answers)
        {
            if (await _featureChecker.IsEnabledAsync(UnityFlex))
            {
                await _localEventBus.PublishAsync(new PersistScoresheetSectionInstanceEto()
                {
                    AssessmentId = assessmentId,
                    AssessmentAnswers = answers
                });
            }
        }

        private async Task<double> GetSelectListAnswerSubtotal(ScoresheetInstanceDto instance, List<Guid> questionIds)
        {
            var existingSelectListQuestions = await _scoresheetAppService.GetSelectListQuestionsAsync(questionIds);
            var existingSelectListQuestionIds = existingSelectListQuestions.Select(a => a.Id).ToList();
            double selectListSubtotal = instance.Answers.Where(a => existingSelectListQuestionIds.Contains(a.QuestionId))
                .Sum(answer =>
                {
                    var value = ValueResolver.Resolve(answer.CurrentValue!, QuestionType.SelectList)!.ToString();
                    var question = existingSelectListQuestions.Find(q => q.Id == answer.QuestionId) ?? throw new AbpValidationException("Missing QuestionId");
                    var definition = JsonSerializer.Deserialize<QuestionSelectListDefinition>(question.Definition ?? "{}");
                    var selectedOption = definition?.Options.Find(o => o.Value == value);
                    if (selectedOption != null)
                    {
                        return selectedOption.NumericValue;
                    }
                    else
                    {
                        return 0;
                    }
                });
            return selectListSubtotal;
        }

        private async Task<double> GetYesNoAnswerSubtotal(ScoresheetInstanceDto instance, List<Guid> questionIds)
        {
            var existingYesNoQuestions = await _scoresheetAppService.GetYesNoQuestionsAsync(questionIds);
            var existingYesNoQuestionIds = existingYesNoQuestions.Select(a => a.Id).ToList();
            double yesNoSubtotal = instance.Answers.Where(a => existingYesNoQuestionIds.Contains(a.QuestionId))
                .Sum(answer =>
                {
                    var value = ValueResolver.Resolve(answer.CurrentValue!, QuestionType.YesNo)!.ToString();
                    var question = existingYesNoQuestions.Find(q => q.Id == answer.QuestionId) ?? throw new AbpValidationException("Missing QuestionId");
                    var definition = JsonSerializer.Deserialize<QuestionYesNoDefinition>(question.Definition ?? "{}");
                    return value switch
                    {
                        "Yes" => Convert.ToDouble(definition?.YesValue ?? 0),
                        "No" => Convert.ToDouble(definition?.NoValue ?? 0),
                        _ => 0,
                    };
                });
            return yesNoSubtotal;
        }

        private async Task<double> GetNumericAnswerSubtotal(ScoresheetInstanceDto instance, List<Guid> questionIds)
        {
            var existingNumericQuestionIds = await _scoresheetAppService.GetNumericQuestionIdsAsync(questionIds);
            double numericSubtotal = instance.Answers.Where(a => existingNumericQuestionIds.Contains(a.QuestionId))
                .Sum(a => Convert.ToDouble(ValueResolver.Resolve(a.CurrentValue!, QuestionType.Number)!.ToString()));
            return numericSubtotal;
        }

        /// <summary>
        /// Parses the stored AI scoresheet answers (JSONB) and writes each AI answer as a
        /// real Answer record on the new human assessment's scoresheet instance.
        /// <para>
        /// Question types are resolved via <see cref="IScoresheetAppService"/> so that each value
        /// is stored in the correct serialized format. SelectList answers are converted from the
        /// AI's 1-based numeric index to the actual option value before being persisted.
        /// Questions not identified as Numeric, YesNo, or SelectList default to TextArea.
        /// </para>
        /// <para>
        /// Answers are persisted by publishing a <see cref="PersistScoresheetSectionInstanceEto"/>
        /// local event, reusing the same pipeline as <see cref="PersistSectionAnswersIfEnabledAsync"/>.
        /// </para>
        /// </summary>
        private async Task CopyAiAnswersToAssessmentAsync(string aiScoresheetAnswers, Guid newAssessmentId)
        {
            var rawAiAnswers = new Dictionary<Guid, string>();
            try
            {
                using var doc = JsonDocument.Parse(aiScoresheetAnswers);
                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    if (!Guid.TryParse(property.Name, out var questionId)) continue;
                    if (property.Value.ValueKind != JsonValueKind.Object) continue;
                    if (!property.Value.TryGetProperty("answer", out var answerProp)) continue;
                    rawAiAnswers[questionId] = answerProp.ToString();
                }
            }
            catch (JsonException)
            {
                return;
            }

            if (rawAiAnswers.Count == 0) return;

            var questionIds = rawAiAnswers.Keys.ToList();
            var numericQuestionIds = (await _scoresheetAppService.GetNumericQuestionIdsAsync(questionIds)).ToHashSet();
            var yesNoQuestions = await _scoresheetAppService.GetYesNoQuestionsAsync(questionIds);
            var selectListQuestions = await _scoresheetAppService.GetSelectListQuestionsAsync(questionIds);
            var yesNoQuestionIds = yesNoQuestions.Select(q => q.Id).ToHashSet();
            var selectListQuestionIds = selectListQuestions.Select(q => q.Id).ToHashSet();

            var assessmentAnswers = new List<AssessmentAnswersEto>();
            foreach (var (questionId, rawAnswer) in rawAiAnswers)
            {
                QuestionType questionType;
                string answer;

                if (numericQuestionIds.Contains(questionId))
                {
                    questionType = QuestionType.Number;
                    answer = rawAnswer;
                }
                else if (yesNoQuestionIds.Contains(questionId))
                {
                    questionType = QuestionType.YesNo;
                    answer = rawAnswer;
                }
                else if (selectListQuestionIds.Contains(questionId))
                {
                    questionType = QuestionType.SelectList;
                    var q = selectListQuestions.Find(x => x.Id == questionId);
                    answer = ConvertNumericAnswerToSelectListValue(rawAnswer, q?.Definition);
                }
                else
                {
                    questionType = QuestionType.TextArea;
                    answer = rawAnswer;
                }

                assessmentAnswers.Add(new AssessmentAnswersEto
                {
                    QuestionId = questionId,
                    Answer = answer,
                    QuestionType = (int)questionType
                });
            }

            if (assessmentAnswers.Count > 0)
            {
                await _localEventBus.PublishAsync(new PersistScoresheetSectionInstanceEto
                {
                    AssessmentId = newAssessmentId,
                    AssessmentAnswers = assessmentAnswers
                });
            }
        }

        /// <summary>
        /// Converts a 1-based numeric index (as returned by the AI for SelectList questions)
        /// to the actual option value defined in the question's JSON definition.
        /// Returns the original value unchanged if parsing fails or the index is out of range.
        /// </summary>
        private static string ConvertNumericAnswerToSelectListValue(string numericAnswer, string? definition)
        {
            if (string.IsNullOrEmpty(definition) || string.IsNullOrEmpty(numericAnswer))
                return numericAnswer;
            try
            {
                if (!int.TryParse(numericAnswer.Trim(), out var optionNumber) || optionNumber <= 0)
                    return numericAnswer;
                var selectListDefinition = JsonSerializer.Deserialize<QuestionSelectListDefinition>(definition);
                if (selectListDefinition?.Options != null && selectListDefinition.Options.Count > 0)
                {
                    var optionIndex = optionNumber - 1;
                    if (optionIndex < selectListDefinition.Options.Count)
                        return selectListDefinition.Options[optionIndex].Value;
                }
            }
            catch (JsonException)
            {
                // Malformed definition — return the raw answer unchanged
            }
            return numericAnswer;
        }
    }
}
