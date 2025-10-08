namespace Unity.Reporting.Configuration
{
    public class UpsertColumnMappingDto
    {
        public UpsertMapRowDto[] Rows { get; set; } = [];
    }

    public class UpsertMapRowDto
    {
        public string PropertyName { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }
}
