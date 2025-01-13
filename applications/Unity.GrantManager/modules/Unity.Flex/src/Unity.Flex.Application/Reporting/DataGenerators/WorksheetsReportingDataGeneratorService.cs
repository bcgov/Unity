using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Values;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Reporting.DataGenerators
{
    [RemoteService(false)]
    public class WorksheetsReportingDataGeneratorService : ApplicationService,
        IReportingDataGeneratorService<Worksheet, WorksheetInstanceValue>
    {
        public string Generate(Worksheet worksheet, WorksheetInstanceValue instanceValue)
        {
            var reportData = new Dictionary<string, List<string>>();
            var reportingKeys = worksheet.ReportColumns.Split(ReportingConsts.ReportFieldDelimiter);

            foreach (var reportKey in reportingKeys)
            {
                reportData.Add(reportKey, []);
            }

            var definitions = worksheet.Sections.SelectMany(s => s.Fields).ToList();

            foreach (var value in instanceValue.Values)
            {
                var definition = definitions.Find(s => s.Key == value.Key);
                if (definition != null)
                {
                    var keyValues = ReportingDataGeneratorFactory
                        .Create(definition, value)
                        .Generate();

                    foreach (var keyValue in from keyValue in keyValues
                                             where reportData.ContainsKey(keyValue.Key)
                                             select keyValue)
                    {
                        reportData[keyValue.Key] = keyValue.Value;
                    }
                }
            }

            return JsonSerializer.Serialize(reportData);
        }
    }
}
