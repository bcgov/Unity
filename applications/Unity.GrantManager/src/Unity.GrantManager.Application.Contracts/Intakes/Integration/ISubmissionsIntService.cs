using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Intakes.Integration
{
    public interface ISubmissionsIntService : IApplicationService
    {
        Task<dynamic?> GetSubmissionDataAsync(Guid chefsFormId, Guid submissionId);
    }
}