using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Tokens
{
    public interface ITenantTokenRepository : IRepository<TenantToken, Guid>
    {
    }
}
