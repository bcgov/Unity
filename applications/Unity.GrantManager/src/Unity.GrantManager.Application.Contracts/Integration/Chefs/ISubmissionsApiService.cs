using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integration.Chefs
{
    public interface ISubmissionsApiService : IApplicationService
    {
        Task<dynamic?> GetSubmissionDataAsync(Guid chefsFormId, Guid submissionId);
    }
}