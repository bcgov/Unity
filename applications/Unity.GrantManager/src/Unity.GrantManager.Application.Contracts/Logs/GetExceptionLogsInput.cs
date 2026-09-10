using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Logs;

public class GetExceptionLogsInput : PagedAndSortedResultRequestDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public ExceptionLogSeverity? Severity { get; set; }
    public string? Filter { get; set; }
}
