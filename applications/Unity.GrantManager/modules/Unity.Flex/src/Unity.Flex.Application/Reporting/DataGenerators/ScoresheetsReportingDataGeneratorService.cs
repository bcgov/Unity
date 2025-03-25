﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets.Enums;
using Unity.Flex.Worksheets.Values;
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
                    if (field.Type == QuestionType.Number) // Type = 1
                    {
                        if (field.Answers != null && field.Answers.Count > 0)
                        {
                            var firstAnswer = field.Answers[0];
                            Console.WriteLine($"  -> Number Type Field. First Answer Question: {field.Answers[0].Question}");
                            if (!string.IsNullOrEmpty(firstAnswer?.CurrentValue))
                            {
                                var obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(firstAnswer.CurrentValue);

                                if (obj != null && obj.TryGetValue("value", out object value) && value is string valueStr && int.TryParse(valueStr, out int result))
                                {
                                    totalScore += result;
                                }
                            }

                        }
                    }
                    else if (field.Type == QuestionType.YesNo) // Type = 6
                    {
                        if (field.Answers != null && field.Answers.Count > 0)
                        {
                            var answer = field.Answers[0];

                            // Parse CurrentValue JSON
                            var currentValueObj = !string.IsNullOrEmpty(answer.CurrentValue)
                                                    ? JsonConvert.DeserializeObject<CurrentValueJson>(answer.CurrentValue)
                                                    : null;
                            string userResponse = currentValueObj?.Value ?? "Unknown";

                            // Parse Definition JSON
                            var definitionObj = (answer.Question?.Definition != null)
                                                 ? JsonConvert.DeserializeObject<QuestionYesNoDefinition>(answer.Question.Definition)
                                                 : null;
                            int yesValue = definitionObj?.YesValue ?? 0;
                            int noValue = definitionObj?.NoValue ?? 0;

                            // Compare values
                            int assignedValue = userResponse.Equals("Yes", StringComparison.OrdinalIgnoreCase) ? yesValue : noValue;

                            Console.WriteLine($"  -> Yes/No Type Field. User Answer: {userResponse}, Assigned Value: {assignedValue}");

                            totalScore = totalScore + assignedValue;
                        }
                    }
                    else if (field.Type == QuestionType.SelectList) // Type = 12
                    {
                        if (field.Answers != null && field.Answers.Count > 0)
                        {
                            var answer = field.Answers[0];

                            // Parse CurrentValue JSON
                            var currentValueObj = !string.IsNullOrEmpty(answer.CurrentValue)
                    ? JsonConvert.DeserializeObject<CurrentValueJson>(answer.CurrentValue)
                    : null;
                            string selectedOption = currentValueObj?.Value ?? "Unknown";

                            // Parse Definition JSON
                            var definitionObj = (answer.Question?.Definition != null)
                                                ? JsonConvert.DeserializeObject<QuestionDefinitionSelectList>(answer.Question.Definition)
                                                : null;

                            // Find the option that matches the selected value
                            var matchedOption = definitionObj?.Options?.FirstOrDefault(opt => opt.Value == selectedOption);

                            if (matchedOption != null)
                            {
                                Console.WriteLine($"  -> Select List Type Field. Selected Option: {selectedOption}, Key: {matchedOption.Key}, Numeric Value: {matchedOption.NumericValue}");
                                totalScore = totalScore + matchedOption.NumericValue;

                            }


                            else
                            {
                                Console.WriteLine("  -> Select List Type Field. No matching option found.");
                            }
                        }
                    }
                }
            }
            return totalScore;
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
        public string Value { get; set; }
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
        public List<SelectListOption> Options { get; set; }
    }

    class SelectListOption
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("numeric_value")]
        public int NumericValue { get; set; }
    }
}
