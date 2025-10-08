using System;
using System.Threading.Tasks;
using Unity.Reporting.Domain.Configuration;

namespace Unity.Reporting.Configuration.FieldsProviders
{
    public interface IFieldsProvider
    {
        Task<FieldPathMetaMapDto> GetFieldsMetadataAsync(Guid correlationId);
        Task<string?> DetectChangesAsync(Guid correlationId, ReportColumnsMap reportColumnsMap);

        string CorrelationProvider { get; }
    }
}
