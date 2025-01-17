using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Reporting.DataGenerators
{
    [RemoteService(false)]
    public class ScoresheetsReportingDataGeneratorService : ApplicationService,
        IReportingDataGeneratorService<Scoresheet, ScoresheetInstance>
    {
        public void GenerateAndSet(Scoresheet scoresheet, ScoresheetInstance instanceValue)
        {
            var reportData = new Dictionary<string, List<string>>();

            var reportingKeys = scoresheet.ReportKeys.Split(ReportingConsts.ReportFieldDelimiter);
            var answers = instanceValue.Answers.ToList();

            foreach (var reportKey in reportingKeys)
            {
                reportData.Add(reportKey, []);
            }

            foreach (var (key, answer) in from key in reportData
                                          let answerKeys = answers.Find(s => s.Question?.Name == key.Key)
                                          select (key, answerKeys))
            {
                if (answer != null)
                {
                    var keyValues = ScoresheetsReportingDataGeneratorFactory
                        .Create(answer)
                        .Generate();

                    foreach (var keyValue in from keyValue in keyValues
                                             where reportData.ContainsKey(keyValue.Key)
                                             select keyValue)
                    {
                        reportData[keyValue.Key] = keyValue.Value;
                    }
                }
            }

            instanceValue.SetReportingData(JsonSerializer.Serialize(reportData));
        }
    }
}
