using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.AI.Setup;

public interface IAIModelAppService : IApplicationService
{
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

    /// <summary>Updates the operation, creating its row when a built-in operation is not configured yet.</summary>
    Task<AIOperationDto> UpdateAsync(string name, UpdateAIOperationDto input);
}
