namespace Unity.Flex.Reporting.FieldGenerators
{
    public interface IReportingFieldsGenerator
    {
        (string columns, string keys) Generate();
    }
}
