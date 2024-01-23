using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.ApplicationForms
{
    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public class ApplicationFormTokenAppService : ApplicationService, IApplicationFormTokenAppService
    {

        public ApplicationFormTokenAppService()
        {
            
        }

        public async Task<string> GenerateApiTokenForFormAsync(Guid formId)
        {
            return await Task.FromResult(Convert.ToBase64String(Guid.NewGuid().ToByteArray()));
        }

        public Task<string> GetApiTokenForFormAsync(Guid formId)
        {
            throw new NotImplementedException();
        }        
    }
}
