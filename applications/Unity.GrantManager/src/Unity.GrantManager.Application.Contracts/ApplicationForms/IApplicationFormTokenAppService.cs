using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.ApplicationForms
{
    public interface IApplicationFormTokenAppService : IApplicationService
    {
        string GenerateFormApiToken();
        Task<string?> GetFormApiTokenAsync();
        Task SetFormApiTokenAsync(string? value);
    }
}
