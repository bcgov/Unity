using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.ApplicationForms
{
    public interface IApplicationFormTokenAppService : IApplicationService
    {
        Task<string> GenerateApiTokenForFormAsync(Guid formId);
        Task<string> GetApiTokenForFormAsync(Guid formId);
    }
}
