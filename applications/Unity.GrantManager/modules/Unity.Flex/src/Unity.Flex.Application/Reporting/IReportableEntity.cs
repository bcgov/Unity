namespace Unity.Flex.Reporting
{
    public interface IReportableEntity<out T>
    {
        string ReportColumns { get; set; }
        string ReportKeys { get; set; }
        string ReportViewName { get; set; }

        T SetReportingFields(string keys, string columns, string reportViewName);
    }
}
