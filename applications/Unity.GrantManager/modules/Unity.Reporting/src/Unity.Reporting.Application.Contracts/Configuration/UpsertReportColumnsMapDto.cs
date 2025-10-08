using System;

namespace Unity.Reporting.Configuration
{
    public class UpsertReportColumnsMapDto
    {
        public Guid CorrelationId { get; set; }
        
        public string CorrelationProvider { get; set; } = string.Empty;
       
        public UpsertColumnMappingDto Mapping { get; set; } = new();
    }
}
