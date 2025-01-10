using System.Collections.Generic;
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
        public string Generate(Scoresheet scoresheet, ScoresheetInstance instanceValue)
        {
            var reportData = new Dictionary<string, List<string>>();

            var keys = scoresheet.ReportKeys.Split(ReportingConsts.ReportFieldDelimiter);

            return JsonSerializer.Serialize(reportData);
        }
    }
}
