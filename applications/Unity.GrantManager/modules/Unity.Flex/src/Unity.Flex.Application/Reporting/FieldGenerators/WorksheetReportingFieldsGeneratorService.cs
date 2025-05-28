using System.Linq;
using System.Text;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;

namespace Unity.Flex.Reporting.FieldGenerators
{
    [RemoteService(false)]
    public class WorksheetReportingFieldsGeneratorService(ILocalEventBus localEventBus) : ApplicationService, IReportingFieldsGeneratorService<Worksheet>
    {
        public Worksheet GenerateAndSet(Worksheet worksheet)
        {
            var (reportingKeys, reportingColumns, reportViewName) = GenerateReportingFields(worksheet);
            worksheet.SetReportingFields(reportingKeys, reportingColumns, reportViewName);

            localEventBus.PublishAsync(new WorksheetsDynamicViewGeneratorEto()
            {
                TenantId = CurrentTenant.Id,
                WorksheetId = worksheet.Id
            }, true);

            return worksheet;
        }

        private static (string reportingKeys, string reportingColumns, string reportViewName) GenerateReportingFields(Worksheet worksheet)
        {
            StringBuilder keysBuilder = new();
            StringBuilder columnsBuilder = new();

            foreach (var field in worksheet.Sections.SelectMany(s => s.Fields))
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

            return new(keysBuilder.ToString(), columnsBuilder.ToString(), $"Worksheet-{worksheet.Name}");
        }
    }
}
