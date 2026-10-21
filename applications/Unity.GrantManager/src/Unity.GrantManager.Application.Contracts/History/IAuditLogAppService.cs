using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.History;

public interface IAuditLogAppService : IApplicationService
{
    Task<PagedResultDto<AuditLogDto>> GetListAsync(GetAuditLogsInput input);
    Task<List<string>> GetEntityTypeFullNamesAsync(GetAuditLogEntityTypesInput input);
}