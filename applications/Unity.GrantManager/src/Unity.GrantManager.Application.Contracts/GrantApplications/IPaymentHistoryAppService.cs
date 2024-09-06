using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.History
{
    public interface IPaymentHistoryAppService : IApplicationService
    {
        Task<List<HistoryDto>> GetPaymentHistoryList(Guid? entityId);
    }
}
