using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integration.Orgbook
{
    public interface IOrgBookService : IApplicationService
    {
        Task<dynamic?> GetOrgBookQueryAsync(string orgBookQuery);
    }
}