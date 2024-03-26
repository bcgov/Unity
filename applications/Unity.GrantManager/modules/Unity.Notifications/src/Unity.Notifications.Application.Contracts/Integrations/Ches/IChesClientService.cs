using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.Integration.Ches
{
    public interface IChesClientService : IApplicationService
    {
        Task SendAsync(Object emailRequest);
    }
}
