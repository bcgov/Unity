namespace Unity.Flex.Reporting.FieldGenerators
{
    public interface IReportingFieldsGeneratorService<T> where T : IReportableEntity<T>
    {
        T GenerateAndSet(T worksheet, char separator = '|', uint maxColumnLength = 63);
    }
}
