using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Identity
{
    public interface ITenantUserRepository : IRepository<User, Guid>
    {
        Task<User?> FindByOidcSub(string sub);        
    }
}
