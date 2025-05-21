using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integrations.Chefs
{
    public interface IFormsApiService : IApplicationService
    {
        Task<dynamic?> GetFormDataAsync(string chefsFormId, string chefsFormVersionId);
        Task<object> GetForm(Guid? formId, string chefsApplicationFormGuid, string encryptedApiKey);
    }
}
