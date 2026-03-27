using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationContentAppService : IApplicationService
{
    Task<string> GenerateContentAsync(Guid applicationId, string? promptVersion = null);
}
