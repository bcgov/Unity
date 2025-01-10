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

            localEventBus.PublishAsync(new WorksheetsDynamicViewGeneratorEto()
            {
                TenantId = CurrentTenant.Id,
                WorksheetId = scoresheet.Id
            }, true);

            return scoresheet;
        }

        private static (string reportingKeys, string reportingColumns, string reportViewName) GenerateReportingFields(Scoresheet scoresheet)
        {
            StringBuilder keysBuilder = new();
            StringBuilder columnsBuilder = new();

            foreach (var field in scoresheet.Sections.SelectMany(s => s.Fields))
            {
                var (columns, keys) = ReportingFieldsGeneratorFactory
                                        .Create(field)
                                        .Generate();

                columnsBuilder.Append(columns).Append(ReportingConsts.ReportFieldDelimiter);
                keysBuilder.Append(keys).Append(ReportingConsts.ReportFieldDelimiter);
            }

            // Remove the trailing separator
            if (columnsBuilder.Length > 0)
            {
                columnsBuilder.Length--;
                // Remove the last separator
            }

            if (keysBuilder.Length > 0)
            {
                keysBuilder.Length--; // Remove the last separator                                    
            }

            return new(columnsBuilder.ToString(), keysBuilder.ToString(), $"Scoresheet-{scoresheet.Name}");
        }
    }
}
