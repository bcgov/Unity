namespace Unity.Flex.Reporting.FieldGenerators
{
    public interface IReportingFieldsGeneratorService<T> where T : IReportableEntity<T>
    {
        T GenerateAndSet(T sheet);
    }
}
