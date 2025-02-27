namespace Unity.Flex.Reporting.DataGenerators
{
    public interface IReportingDataGeneratorService<in T, in U> where T : IReportableEntity<T>
    {
        void GenerateAndSet(T sheet, U instance);
    }
}
