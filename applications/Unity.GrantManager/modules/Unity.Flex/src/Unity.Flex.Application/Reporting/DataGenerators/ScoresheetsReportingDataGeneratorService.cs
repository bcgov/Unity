﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
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

                instanceValue.SetReportingData(JsonSerializer.Serialize(reportData));
            }
            catch (Exception ex)
            {
                // Blanket catch here, as we dont want this generation to interfere we intake, report formatted data can be re-generated later
                Logger.LogError(ex, "Error processing reporting data for scoresheet - correlationId: {CorrelationId}", instanceValue.CorrelationId);
            }
        }
    }
}
