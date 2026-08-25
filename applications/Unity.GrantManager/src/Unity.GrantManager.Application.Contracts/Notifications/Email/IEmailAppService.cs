using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.Notifications.Email
{
    public interface IEmailAppService
    {
        Task<bool> SendAsync(CreateEmailDto dto);
        Task<bool> SaveDraftAsync(CreateEmailDto dto);
        Task<Guid> InitializeDraftAsync(Guid applicationId);
    }
}
