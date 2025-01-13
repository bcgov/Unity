using Volo.Abp.Application.Services;
using Volo.Abp;
using Volo.Abp.EventBus.Local;
using Unity.Flex.Domain.Scoresheets;
using System.Text;
using System.Linq;

namespace Unity.Flex.Reporting.FieldGenerators
{
    [RemoteService(false)]
    public class ScoresheetReportingFieldsGeneratorService(ILocalEventBus localEventBus) : ApplicationService,
        IReportingFieldsGeneratorService<Scoresheet>
    {
        public Scoresheet GenerateAndSet(Scoresheet scoresheet)
        {
            var (reportingKeys, reportingColumns, reportViewName) = GenerateReportingFields(scoresheet);
            scoresheet.SetReportingFields(reportingKeys, reportingColumns, reportViewName);

            localEventBus.PublishAsync(new ScoresheetsDynamicViewGeneratorEto()
            {
                TenantId = CurrentTenant.Id,
                ScoresheetId = scoresheet.Id
            }, true);

            return scoresheet;
        }

        private static (string reportingKeys, string reportingColumns, string reportViewName) GenerateReportingFields(Scoresheet scoresheet)
        {
            StringBuilder keysBuilder = new();
            StringBuilder columnsBuilder = new();

            foreach (var field in scoresheet.Sections.SelectMany(s => s.Fields))
            {
                var (keys, columns) = ReportingFieldsGeneratorFactory
                                        .Create(field)
                                        .Generate();

                keysBuilder
                    .Append(keys)
                    .Append(ReportingConsts.ReportFieldDelimiter);

                columnsBuilder
                    .Append(columns)
                    .Append(ReportingConsts.ReportFieldDelimiter);
            }

            keysBuilder.TrimEndDelimeter();
            columnsBuilder.TrimEndDelimeter();

            return new(keysBuilder.ToString(), columnsBuilder.ToString(), $"Scoresheet-{scoresheet.Name}");
        }
    }
}
