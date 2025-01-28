using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Reporting.DataGenerators;
using Unity.Flex.Scoresheets.Enums;
using Unity.Flex.Scoresheets.Events;
using Unity.Flex.Worksheets.Definitions;
using Unity.Modules.Shared.Features;
using Volo.Abp.Domain.Services;
using Volo.Abp.Features;
using Volo.Abp.Validation;

namespace Unity.Flex.Domain.Services
{
    public class ScoresheetsManager(IScoresheetInstanceRepository scoresheetInstanceRepository,
        IScoresheetRepository scoresheetRepository,
        IReportingDataGeneratorService<Scoresheet, ScoresheetInstance> reportingDataGeneratorService,
        IFeatureChecker featureChecker) : DomainService
    {
        public static List<string> ValidateScoresheetAnswersAsync(ScoresheetInstance scoresheetInstance, Scoresheet scoresheet)
        {
            var errors = new List<string>();
            var scoresheetAnswers = scoresheetInstance.Answers.ToList();

            foreach (Question? question in scoresheet.Sections.SelectMany(s => s.Fields))
            {
                ValidateRequiredQuestions(errors, scoresheetAnswers, question);
            }

            return errors;
        }

        private static void ValidateRequiredQuestions(List<string> errors, List<Answer> scoresheetAnswers, Question? question)
        {
            if (question == null) return;

            if (question.IsRequired())
            {
                var answer = scoresheetAnswers.Find(s => s.QuestionId == question.Id);
                if (answer == null || !answer.IsProvided()) errors.Add(BuildMissingAnswerError(question));
            }
        }

        private static string BuildMissingAnswerError(Question question)
        {
            return $"{question.Section?.Order + 1}.{question.Order + 1}: {question.Label} (required)";
        }

        public async Task PersistScoresheetData(PersistScoresheetSectionInstanceEto eventData)
        {
            var instance = await scoresheetInstanceRepository.GetByCorrelationAsync(eventData.AssessmentId) ?? throw new AbpValidationException("Missing ScoresheetInstance.");
            var scoresheet = await scoresheetRepository.GetAsync(instance.ScoresheetId);

            var scoresheetAnswers = eventData.AssessmentAnswers.ToList();

            foreach (var item in scoresheetAnswers)
            {
                var ans = instance.Answers.FirstOrDefault(a => a.QuestionId == item.QuestionId);

                if (ans != null)
                {
                    ans.SetValue(ValueConverter.Convert(item.Answer ?? "", (QuestionType)item.QuestionType));
                }
                else
                {
                    ans = new Answer(Guid.NewGuid())
                    {
                        CurrentValue = ValueConverter.Convert(item?.Answer?.ToString() ?? string.Empty, (QuestionType)item!.QuestionType),
                        QuestionId = item.QuestionId,
                        ScoresheetInstanceId = instance.Id
                    };
                    instance.Answers.Add(ans);
                }

                await scoresheetInstanceRepository.UpdateAsync(instance);
            }

            if (await featureChecker.IsEnabledAsync(FeatureConsts.Reporting))
            {
                reportingDataGeneratorService.GenerateAndSet(scoresheet, instance);
            }
        }
    }

    public static class ScorehseetExtensions
    {
        public static bool IsRequired(this Question question)
        {
            CustomFieldDefinition? fieldDefinition = question.Definition.ConvertDefinition(question.Type);
            if (fieldDefinition == null) return false;
            return DefinitionResolver.ResolveIsRequired(fieldDefinition);
        }

        public static bool IsProvided(this Answer answer)
        {
            if (answer.Question == null) return false;
            var currentValue = ValueResolver.Resolve(answer.CurrentValue ?? string.Empty, answer.Question.Type);
            if (currentValue == null) return false;
            if (string.IsNullOrEmpty(currentValue.ToString())) return false;
            return true;
        }
    }
}
