using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.Intakes.Integration
{
    public interface IFormIntService : IApplicationService
    {
        Task<dynamic?> GetFormDataAsync(string chefsFormId, string chefsFormVersionId);
        Task<object> GetForm(Guid? formId, string chefsApplicationFormGuid, string encryptedApiKey);
    }
}
