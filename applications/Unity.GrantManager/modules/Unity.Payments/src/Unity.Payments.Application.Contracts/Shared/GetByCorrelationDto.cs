using System;

namespace Unity.Payments.Shared
{
    public class GetByCorrelationDto
    {
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = null!;
        public bool IncludeDetails { get; set; }
    }
}
