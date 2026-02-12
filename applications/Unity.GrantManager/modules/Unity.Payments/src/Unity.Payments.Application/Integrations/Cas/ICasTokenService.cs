using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
namespace Unity.Payments.Integrations.Cas
{
    public interface ICasTokenService : IApplicationService
    {
        Task<string> GetAuthTokenAsync(Guid tenantId);
    }
}