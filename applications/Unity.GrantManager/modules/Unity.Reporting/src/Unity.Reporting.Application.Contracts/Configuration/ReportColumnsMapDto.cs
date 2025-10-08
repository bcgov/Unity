using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Reporting.Configuration
{
    public class ReportColumnsMapDto : EntityDto<Guid>
    {
        public Guid CorrelationId { get; set; }

        public string CorrelationProvider { get; set; } = string.Empty;

        public MappingDto Mapping { get; set; } = new();

        public Guid? TenantId { get; set; }

        public string ViewName { get; set; } = string.Empty;

        public ViewStatus ViewStatus { get; set; } = ViewStatus.GENERATING;
        public RoleStatus RoleStatus { get; set; } = RoleStatus.NOTASSIGNED;

        public string? DetectedChanges { get; set; } = null;
    }

    public class MappingDto
    {
        public MapRowDto[] Rows { get; set; } = [];
    }

    public class MapRowDto
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
}
