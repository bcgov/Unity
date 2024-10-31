using System.Collections.Generic;
using System.Linq;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Worksheets.Definitions;
using Volo.Abp.Domain.Services;

namespace Unity.Flex.Domain.Services
{
    public class ScoresheetsManager : DomainService
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
