using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Logs;

public interface IExceptionLogAppService : IApplicationService
{
    [RemoteService(false)]
    Task<Guid> CreateAsync(CreateExceptionLogDto input);

    Task<PagedResultDto<ExceptionLogDto>> GetListAsync(GetExceptionLogsInput input);
}
