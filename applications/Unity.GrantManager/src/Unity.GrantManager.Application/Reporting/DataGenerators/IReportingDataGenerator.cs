namespace Unity.GrantManager.Reporting.DataGenerators
{
    public interface IReportingDataGenerator
    {
        string? Generate(dynamic formSubmission, string? reportKeys);
    }
}
