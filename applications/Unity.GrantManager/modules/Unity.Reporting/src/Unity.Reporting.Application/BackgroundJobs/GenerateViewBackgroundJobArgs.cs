using System;

namespace Unity.Reporting.BackgroundJobs
{
    public class GenerateViewBackgroundJobArgs
    {
        public string CorrelationProvider { get; set; } = string.Empty;
        public Guid CorrelationId { get; set; }         
        public Guid? TenantId { get; set; }
        public string OriginalViewName { get; set; } = string.Empty;
    }
}
