using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Identity
{
    public interface IPersonRepository : IRepository<Person, Guid>
    {
        Task<Person?> FindByOidcSub(string oidcSub);        
    }
}
