using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Unity.Reporting.Configuration;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Reporting.Domain.Configuration
{
    public class ReportColumnsMap : AuditedEntity<Guid>, IMultiTenant
    {
        public Guid CorrelationId { get; set; }

        public string CorrelationProvider { get; set; } = string.Empty;

        [Column(TypeName = "jsonb")]
        public string Mapping { get; set; } = "{}";

        public Guid? TenantId { get; set; }
        public string ViewName { get; set; } = string.Empty;

        public ViewStatus ViewStatus { get; set; } = ViewStatus.GENERATING;
        public RoleStatus RoleStatus { get; set; } = RoleStatus.NOTASSIGNED;

    }

    public class Mapping
    {
        public MapRow[] Rows { get; set; } = [];
        public MapMetadata? Metadata { get; set; } = null;
    }

    public class MapRow
    {
        public string Label { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string DataPath { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string TypePath { get; set; } = string.Empty;
    }

    public class MapMetadata
    {
        public Dictionary<string, string>? Info { get; set; } = null;
    }
}
