using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Reporting.DataGenerators
{
    public interface IReportingDataGeneratorService
    {
        string GenerateData(Worksheet worksheet, WorksheetInstanceValue instanceCurrentValue);
    }
}
