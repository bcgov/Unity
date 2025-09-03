using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integrations.Chefs
{
    public interface IFormsApiService : IApplicationService
    {
        Task<JObject?> GetFormDataAsync(string chefsFormId, string chefsFormVersionId);
        Task<JObject> GetForm(Guid? formId, string chefsApplicationFormGuid, string encryptedApiKey);
        Task<JObject?> GetSubmissionDataAsync(Guid chefsFormId, Guid submissionId);
    }
}
