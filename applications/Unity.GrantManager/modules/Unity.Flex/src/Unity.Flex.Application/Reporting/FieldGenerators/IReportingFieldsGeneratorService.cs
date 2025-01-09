using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Reporting.FieldGenerators
{
    public interface IReportingFieldsGeneratorService
    {
        Worksheet GenerateAndSet(Worksheet worksheet, char separator = '|', uint maxColumnLength = 63);
    }
}
