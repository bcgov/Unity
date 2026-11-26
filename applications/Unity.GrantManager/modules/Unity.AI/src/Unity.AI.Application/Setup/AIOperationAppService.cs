using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Operations;
using Unity.AI.Runtime.Prompts;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.AI.Setup;

/// <summary>
/// Host-wide AI operations for Unity AI Setup: which model, token limit, execution mode and
/// on/off each built-in operation uses. IT Operations only.
/// </summary>
[Authorize(IdentityConsts.ITAdminOrITOperationsPolicyName)]
[Route("api/app/ai/setup/operations")]
public class AIOperationAppService(
    IRepository<AIOperation, Guid> operationRepository,
    IRepository<AIModel, Guid> modelRepository,
    IRepository<AIPrompt, Guid> promptRepository,
    IDataFilter<IMultiTenant> multiTenantDataFilter,
    AISetupManager setupManager) : ApplicationService, IAIOperationAppService
{
    [HttpGet]
    public virtual async Task<ListResultDto<AIOperationDto>> GetListAsync()
    {
        var operations = await operationRepository.GetListAsync();
        var models = (await modelRepository.GetListAsync()).ToDictionary(model => model.Id);
        var promptVersions = await GetHostPromptVersionsAsync();

        var items = new List<AIOperationDto>();
        foreach (var name in AIPromptTypes.All)
        {
            var operation = operations.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
            items.Add(ToDto(name, operation, models, promptVersions, isBuiltIn: true));
        }

        // Rows that aren't built-in operations are listed so IT Operations can see them.
        // AI Reporting's rows have their own section (GetAIReportingAsync).
        foreach (var operation in operations
                     .Where(candidate => AISetupManager.FindBuiltInName(candidate.Name) == null)
                     .OrderBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase))
        {
            items.Add(ToDto(operation.Name, operation, models, promptVersions, isBuiltIn: false));
        }

        return new ListResultDto<AIOperationDto>(items);
    }

    [HttpGet("ai-reporting")]
    public virtual async Task<ListResultDto<AIOperationDto>> GetAIReportingAsync()
    {
        var operations = await operationRepository.GetListAsync();
        var models = (await modelRepository.GetListAsync()).ToDictionary(model => model.Id);
        var noPrompts = new Dictionary<string, int>();

        var items = AISetupConsts.AIReportingOperations
            .Select(name => ToDto(
                name,
                operations.FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)),
                models,
                noPrompts,
                isBuiltIn: true))
            .ToList();

        return new ListResultDto<AIOperationDto>(items);
    }

    [HttpGet("{name}")]
    public virtual async Task<AIOperationDto> GetAsync(string name)
    {
        var operation = await setupManager.FindOperationAsync(name);
        var builtInName = AISetupManager.FindBuiltInName(name);
        if (operation == null && builtInName == null)
        {
            throw new EntityNotFoundException(typeof(AIOperation), name);
        }

        var models = (await modelRepository.GetListAsync()).ToDictionary(model => model.Id);
        var promptVersions = await GetHostPromptVersionsAsync();
        return ToDto(operation?.Name ?? builtInName!, operation, models, promptVersions, isBuiltIn: builtInName != null);
    }

    [HttpPut("{name}")]
    [Authorize(IdentityConsts.ITOperationsPolicyName)]
    public virtual async Task<AIOperationDto> UpdateAsync(string name, UpdateAIOperationDto input)
    {
        var operation = await setupManager.SetOperationAsync(
            name,
            input.AIModelId,
            input.ExecutionMode,
            input.IsActive);

        var models = (await modelRepository.GetListAsync()).ToDictionary(model => model.Id);
        var promptVersions = await GetHostPromptVersionsAsync();
        var isBuiltIn = AISetupManager.FindBuiltInName(operation.Name) != null;
        return ToDto(operation.Name, operation, models, promptVersions, isBuiltIn);
    }

    /// <summary>
    /// Highest active host prompt version per prompt family, which is what the runtime resolves
    /// for a tenant without its own prompt.
    /// </summary>
    private async Task<Dictionary<string, int>> GetHostPromptVersionsAsync()
    {
        using (multiTenantDataFilter.Disable())
        {
            var prompts = await promptRepository.GetListAsync(prompt => prompt.TenantId == null && prompt.IsActive);
            return prompts
                .GroupBy(prompt => prompt.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Max(prompt => prompt.VersionNumber),
                    StringComparer.OrdinalIgnoreCase);
        }
    }

    private static AIOperationDto ToDto(
        string name,
        AIOperation? operation,
        IReadOnlyDictionary<Guid, AIModel> models,
        IReadOnlyDictionary<string, int> promptVersions,
        bool isBuiltIn)
    {
        var dto = new AIOperationDto
        {
            Name = name,
            IsBuiltIn = isBuiltIn,
            RequiredModelType = AISetupManager.GetRequiredModelType(name).ToString(),
            UsesExecutionMode = AIExecutionModeResolver.OperationsUsingExecutionMode.Contains(name, StringComparer.OrdinalIgnoreCase),
            PromptVersion = promptVersions.TryGetValue(name, out var version) ? version.ToString(CultureInfo.InvariantCulture) : null
        };

        if (operation == null)
        {
            return dto;
        }

        models.TryGetValue(operation.AIModelId, out var model);
        dto.Id = operation.Id;
        dto.IsConfigured = true;
        dto.AIModelId = operation.AIModelId;
        dto.AIModelName = model?.Name;
        dto.AIModelIsActive = model?.IsActive ?? false;
        dto.ExecutionMode = operation.ExecutionMode.ToString();
        dto.IsActive = operation.IsActive;
        dto.LastModificationTime = operation.LastModificationTime ?? operation.CreationTime;
        return dto;
    }
}
