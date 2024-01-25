using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Identity;

namespace Unity.GrantManager.Identity
{
    public interface IUserAccountsRepository
    {
        Task<IList<IdentityUser>> GetListByOidcSub(string oidcSub);
    }
}
