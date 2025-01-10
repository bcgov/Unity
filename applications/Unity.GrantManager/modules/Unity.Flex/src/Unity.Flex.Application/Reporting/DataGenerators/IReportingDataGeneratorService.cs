namespace Unity.Flex.Reporting.DataGenerators
{
    public interface IReportingDataGeneratorService<in T, in U> where T : IReportableEntity<T>
    {
        string Generate(T sheet, U instance);
    }
}
