using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.Notifications.Email
{
    public interface IEmailAppService
    {
        Task<bool> CreateAsync(CreateEmailDto dto);
        Task<Guid> InitializeDraftAsync(Guid applicationId);
    }
}
