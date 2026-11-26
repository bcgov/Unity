using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Runtime.Execution;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Unity.AI.Setup;

/// <summary>
/// Host-wide AI models (Azure OpenAI deployments) for Unity AI Setup. IT Operations only.
/// </summary>
// Both roles can see and Test models; only IT Administrator adds, edits (including
// activate/deactivate) or deletes them, since a model mirrors an Azure deployment.
[Authorize(IdentityConsts.ITAdminOrITOperationsPolicyName)]
[Route("api/app/ai/setup/models")]
public class AIModelAppService(
    IRepository<AIModel, Guid> modelRepository,
    IRepository<AIOperation, Guid> operationRepository,
    AISetupManager setupManager,
    AIModelConnectionTester connectionTester) : ApplicationService, IAIModelAppService
{

    [HttpGet]
    public virtual async Task<ListResultDto<AIModelDto>> GetListAsync()
    {
        var models = await modelRepository.GetListAsync();
        var operations = await operationRepository.GetListAsync();
        var items = models
            .OrderBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
            .Select(model => ToDto(model, operations))
            .ToList();
        return new ListResultDto<AIModelDto>(items);
    }

    [HttpGet("{id}")]
    public virtual async Task<AIModelDto> GetAsync(Guid id)
    {
        var model = await modelRepository.GetAsync(id);
        var operations = await operationRepository.GetListAsync(operation => operation.AIModelId == id);
        return ToDto(model, operations);
    }

    [HttpPost]
    [Authorize(IdentityConsts.ITAdminPolicyName)]
    public virtual async Task<AIModelDto> CreateAsync(CreateUpdateAIModelDto input)
    {
        var model = await setupManager.CreateModelAsync(
            input.Name,
            input.Provider,
            input.IsActive);
        return ToDto(model, []);
    }

    [HttpPut("{id}")]
    [Authorize(IdentityConsts.ITAdminPolicyName)]
    public virtual async Task<AIModelDto> UpdateAsync(Guid id, CreateUpdateAIModelDto input)
    {
        var model = await modelRepository.GetAsync(id);
        model = await setupManager.UpdateModelAsync(
            model,
            input.Name,
            input.Provider,
            input.IsActive);
        var operations = await operationRepository.GetListAsync(operation => operation.AIModelId == id);
        return ToDto(model, operations);
    }

    [HttpDelete("{id}")]
    [Authorize(IdentityConsts.ITAdminPolicyName)]
    public virtual async Task DeleteAsync(Guid id)
    {
        var model = await modelRepository.GetAsync(id);
        await setupManager.DeleteModelAsync(model);
    }

    [HttpPost("{id}/test")]
    public virtual async Task<AIModelTestResultDto> TestAsync(Guid id)
    {
        var model = await modelRepository.GetAsync(id);
        return await connectionTester.TestAsync(model);
    }

    private static AIModelDto ToDto(AIModel model, IEnumerable<AIOperation> operations)
    {
        return new AIModelDto
        {
            Id = model.Id,
            Name = model.Name,
            Provider = model.Provider,
            IsActive = model.IsActive,
            UsedByOperations = operations
                .Where(operation => operation.AIModelId == model.Id)
                .Select(operation => operation.Name)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            CreationTime = model.CreationTime,
            CreatorId = model.CreatorId,
            LastModificationTime = model.LastModificationTime,
            LastModifierId = model.LastModifierId
        };
    }
}
