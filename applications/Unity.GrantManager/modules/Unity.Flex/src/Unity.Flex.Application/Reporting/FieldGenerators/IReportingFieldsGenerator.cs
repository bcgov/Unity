namespace Unity.Flex.Reporting.FieldGenerators
{
    public interface IReportingFieldsGenerator
    {
        (string keys, string columns) Generate();
    }
}
