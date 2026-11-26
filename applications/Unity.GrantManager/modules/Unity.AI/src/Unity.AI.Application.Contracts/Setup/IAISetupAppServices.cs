using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.AI.Setup;

public interface IAIModelAppService : IApplicationService
{
    /// <summary>Calls the Azure OpenAI endpoint's /healthz from the server (works for the internal proxy).</summary>
    Task<AIEndpointStatusDto> GetEndpointStatusAsync();


    Task<ListResultDto<AIModelDto>> GetListAsync();

    Task<AIModelDto> GetAsync(Guid id);

    Task<AIModelDto> CreateAsync(CreateUpdateAIModelDto input);

    Task<AIModelDto> UpdateAsync(Guid id, CreateUpdateAIModelDto input);

    Task DeleteAsync(Guid id);

    /// <summary>Makes one small chat call to the model's deployment with the configured endpoint and key.</summary>
    Task<AIModelTestResultDto> TestAsync(Guid id);
}

public interface IAIOperationAppService : IApplicationService
{
    /// <summary>All built-in operations (configured or not), plus any other configured rows.</summary>
    Task<ListResultDto<AIOperationDto>> GetListAsync();

    Task<AIOperationDto> GetAsync(string name);

    /// <summary>AI Reporting's chat and embedding model rows (configured or not).</summary>
    Task<ListResultDto<AIOperationDto>> GetAIReportingAsync();

    /// <summary>Updates the operation, creating its row when a built-in operation is not configured yet.</summary>
    Task<AIOperationDto> UpdateAsync(string name, UpdateAIOperationDto input);
}
