using System;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integration.Chefs
{
    public interface ISubmissionsApiService : IApplicationService
    {
        Task<JsonDocument?> GetSubmissionDataAsync(Guid chefsFormId, Guid submissionId);
    }
}