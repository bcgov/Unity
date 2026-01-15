using System;

namespace Unity.Modules.Shared.Correlation
{
    public record Correlation(Guid CorrelationId, string CorrelationProvider);    
}
