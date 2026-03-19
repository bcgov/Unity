using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationAIContentAppService : IApplicationService
{
    Task<string> GenerateAIContentAsync(Guid applicationId, string? promptVersion = null);
}

