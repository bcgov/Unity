using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integrations
{
    public interface ICasClientCodeLookupService : IApplicationService
    {
        Task<List<CasClientCodeOptionDto>> GetActiveOptionsAsync();
        Task<string?> GetClientIdByCasClientCodeAsync(string casClientCode);
    }
}