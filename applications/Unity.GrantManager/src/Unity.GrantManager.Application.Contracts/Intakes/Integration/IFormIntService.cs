using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Intakes.Integration
{
    public interface IFormIntService : IApplicationService
    {
        Task<dynamic?> GetFormDataAsync(string chefsFormId, string chefsFormVersionId);
        Task<object> GetForm(Guid? formId, string chefsApplicationFormGuid, string encryptedApiKey);
    }
}
