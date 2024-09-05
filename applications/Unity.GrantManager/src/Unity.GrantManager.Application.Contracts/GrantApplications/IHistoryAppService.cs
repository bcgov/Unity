using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.History
{
    public interface IHistoryAppService : IApplicationService
    {
        Task<List<HistoryDto>> GetHistoryList(string? entityId, string filterPropertyName, Dictionary<string, string>? lookupDictionary);
        Task<string> LookupUserName(Guid auditLogId);
    }
}
