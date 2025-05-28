using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets.Enums;
using Volo.Abp;

namespace Unity.Flex.Reporting.DataGenerators
{
    [RemoteService(false)]
    public class ScoresheetsReportingDataGeneratorService : ReportingDataGeneratorServiceBase,
        IReportingDataGeneratorService<Scoresheet, ScoresheetInstance>
    {
        public void GenerateAndSet(Scoresheet scoresheet, ScoresheetInstance instanceValue)
        {
            try
            {
                var reportData = new Dictionary<string, object?>();

                var reportingKeys = scoresheet.ReportKeys.Split(ReportingConsts.ReportFieldDelimiter);
                var answers = instanceValue.Answers.ToList();

                foreach (var reportKey in reportingKeys)
                {
                    reportData.Add(reportKey, null);
                }

                foreach (var (_, answer) in from key in reportData
                                            let answerKeys = answers.Find(s => s.Question?.Name == key.Key)
                                            select (key, answerKeys))
                {
                    if (answer != null)
                    {
                        var keyValues = ScoresheetsReportingDataGeneratorFactory
                            .Create(answer)
                            .Generate();

                        ExtractKeyValueData(reportData, keyValues);
                    }
                }




                var totalScore = CalculateTotalScore(scoresheet);
                reportData.Add("TotalScore", totalScore);

                instanceValue.SetReportingData(System.Text.Json.JsonSerializer.Serialize(reportData));
            }
            catch (Exception ex)
            {
                // Blanket catch here, as we dont want this generation to interfere we intake, report formatted data can be re-generated later
                Logger.LogError(ex, "Error processing reporting data for scoresheet - correlationId: {CorrelationId}", instanceValue.CorrelationId);
            }
        }

        public static int CalculateTotalScore(Scoresheet scoresheet)
        {
            var totalScore = 0;

            foreach (var section in scoresheet.Sections)
            {
                foreach (var field in section.Fields)
                {
                    if (field.Type == QuestionType.Number)
                    {
                        totalScore += CalculateNumberFieldScore(field);
                    }
                    else if (field.Type == QuestionType.YesNo)
                    {
                        totalScore += CalculateYesNoFieldScore(field);
                    }
                    else if (field.Type == QuestionType.SelectList)
                    {
                        totalScore += CalculateSelectListFieldScore(field);
                    }
                }
            }

            return totalScore;
        }

        private static int CalculateNumberFieldScore(Question field)
        {
            if (field.Answers == null || field.Answers.Count == 0)
                return 0;

            var firstAnswer = field.Answers[0];
            if (string.IsNullOrEmpty(firstAnswer?.CurrentValue))
                return 0;

            var obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(firstAnswer.CurrentValue);

            if (obj != null && obj.TryGetValue("value", out object? value) && value is string valueStr && int.TryParse(valueStr, out int result))
            {
                return result;
            }

            return 0;
        }

        private static int CalculateYesNoFieldScore(Question field)
        {
            if (field.Answers == null || field.Answers.Count == 0)
                return 0;

            var answer = field.Answers[0];
            var currentValueObj = !string.IsNullOrEmpty(answer.CurrentValue)
                                    ? JsonConvert.DeserializeObject<CurrentValueJson>(answer.CurrentValue)
                                    : null;
            string userResponse = currentValueObj?.Value ?? "Unknown";

            var definitionObj = (answer.Question?.Definition != null)
                                 ? JsonConvert.DeserializeObject<QuestionYesNoDefinition>(answer.Question.Definition)
                                 : null;
            int yesValue = definitionObj?.YesValue ?? 0;
            int noValue = definitionObj?.NoValue ?? 0;

            return userResponse.Equals("Yes", StringComparison.OrdinalIgnoreCase) ? yesValue : noValue;
        }

        private static int CalculateSelectListFieldScore(Question field)
        {
            if (field.Answers == null || field.Answers.Count == 0)
                return 0;

            var answer = field.Answers[0];
            var currentValueObj = !string.IsNullOrEmpty(answer.CurrentValue)
                                    ? JsonConvert.DeserializeObject<CurrentValueJson>(answer.CurrentValue)
                                    : null;
            string selectedOption = currentValueObj?.Value ?? "Unknown";

            var definitionObj = (answer.Question?.Definition != null)
                                ? JsonConvert.DeserializeObject<QuestionDefinitionSelectList>(answer.Question.Definition)
                                : null;

            var matchedOption = definitionObj?.Options?.FirstOrDefault(opt => opt.Value == selectedOption);

            return matchedOption?.NumericValue ?? 0;

        }
    }

        public class CurrentValue
    {
        [JsonPropertyName("value")]
        public int Value { get; set; }
    }

    class CurrentValueJson
    {
        [JsonProperty("value")]
        public string? Value { get; set; }
    }

    class QuestionYesNoDefinition 
    {
        [JsonProperty("yes_value")]
        public int YesValue { get; set; }

        [JsonProperty("no_value")]
        public int NoValue { get; set; }
    }

    class QuestionDefinitionSelectList
    {
        [JsonProperty("options")]
        public List<SelectListOption>? Options { get; set; }
    }

    class SelectListOption
    {
        [JsonProperty("key")]
        public string? Key { get; set; }

        [JsonProperty("value")]
        public string? Value { get; set; }

        [JsonProperty("numeric_value")]
        public int NumericValue { get; set; }
    }
}
