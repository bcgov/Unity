namespace Unity.Flex.Worksheets.Reporting.FieldGenerators
{
    public interface IReportingFieldsGenerator
    {
        (string columns, string keys) Generate();
    }
}
