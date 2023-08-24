using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.Account
{
    public interface IUserAccountProfileAppService
    {
        Task<Object> GetAsync();
    }
}
