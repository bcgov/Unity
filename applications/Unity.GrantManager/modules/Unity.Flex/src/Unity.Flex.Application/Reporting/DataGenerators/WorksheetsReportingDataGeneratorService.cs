using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp;

namespace Unity.Flex.Reporting.DataGenerators
{
    [RemoteService(false)]
    public class WorksheetsReportingDataGeneratorService : ReportingDataGeneratorServiceBase,
        IReportingDataGeneratorService<Worksheet, WorksheetInstance>
    {
        /// <summary>
        /// Generates the data for the Worksheet ReportData field
        /// </summary>
        /// <param name="worksheet"></param>
        /// <param name="instanceValue"></param>
        /// <returns>Serialized Key/Values Pair of values for ReportData</returns>
        public void GenerateAndSet(Worksheet worksheet, WorksheetInstance instanceValue)
        {
            try
            {
                var reportData = new Dictionary<string, object?>();
                var reportingKeys = worksheet.ReportKeys.Split(ReportingConsts.ReportFieldDelimiter);

                foreach (var reportKey in reportingKeys)
                {
                    reportData.Add(reportKey, null);
                }

                var definitions = worksheet.Sections.SelectMany(s => s.Fields).ToList();

                foreach (var value in instanceValue.Values)
                {
                    var definition = definitions.Find(s => s.Id == value.CustomFieldId);
                    if (definition != null)
                    {
                        var keyValues = WorksheetsReportingDataGeneratorFactory
                            .Create(definition, value)
                            .Generate();

                        ExtractKeyValueData(reportData, keyValues);
                    }
                }

                instanceValue.SetReportingData(JsonSerializer.Serialize(reportData));
            }
            catch (Exception ex)
            {
                // Blanket catch here, as we dont want this generation to interfere we intake, report formatted data can be re-generated later
                Logger.LogError(ex, "Error processing reporting data for worksheet - correlationId: {CorrelationId}", instanceValue.CorrelationId);
            }
        }
    }
}
