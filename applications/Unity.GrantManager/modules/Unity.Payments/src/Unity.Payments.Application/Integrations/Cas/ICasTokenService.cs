using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;

namespace Unity.Payments.Integrations.Cas
{
    public interface ICasTokenService : IApplicationService
    {
        [AllowAnonymous]
        [RemoteService(false)]
        Task<string> GetAuthTokenAsync(Guid tenantId);
    }
}