using System;

namespace Unity.Reporting.BackgroundJobs
{
    public class AssignViewRoleBackgroundJobArgs
    {
        public string CorrelationProvider { get; set; } = string.Empty;
        public Guid CorrelationId { get; set; }
        public Guid? TenantId { get; set; }
    }
}
