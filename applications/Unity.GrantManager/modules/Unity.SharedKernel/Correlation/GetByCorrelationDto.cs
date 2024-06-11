using System;

namespace Unity.Modules.Shared.Correlation
{
    public class GetByCorrelationDto
    {
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = null!;
        public bool IncludeDetails { get; set; }
    }
}
