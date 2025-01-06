namespace Unity.Flex.Worksheets.Reporting
{
    public interface IReportingFieldsGenerator
    {
        (string columns, string keys) Generate();
    }
}
