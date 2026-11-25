using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Runtime.Execution;
using Unity.Modules.Shared.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Authorization;

namespace Unity.AI.Prompts;

[Authorize(IdentityConsts.ITOperationsPolicyName)]
[Route("api/app/ai/prompts")]
public class AIPromptAppService :
    CrudAppService<
        AIPrompt,
        AIPromptDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateAIPromptDto>,
    IAIPromptAppService
{
    private readonly IDataFilter<IMultiTenant> _multiTenantDataFilter;
    private readonly ICurrentTenant _currentTenant;
    private readonly AIConfigurationCacheSignal _cacheSignal;

    public AIPromptAppService(
        IRepository<AIPrompt, Guid> repository,
        IDataFilter<IMultiTenant> multiTenantDataFilter,
        ICurrentTenant currentTenant,
        AIConfigurationCacheSignal cacheSignal)
        : base(repository)
    {
        _multiTenantDataFilter = multiTenantDataFilter;
        _currentTenant = currentTenant;
        _cacheSignal = cacheSignal;
        GetPolicyName = IdentityConsts.ITOperationsPolicyName;
        GetListPolicyName = IdentityConsts.ITOperationsPolicyName;
        CreatePolicyName = IdentityConsts.ITOperationsPolicyName;
        UpdatePolicyName = IdentityConsts.ITOperationsPolicyName;
        DeletePolicyName = IdentityConsts.ITOperationsPolicyName;
    }

    [HttpGet("by-prompt/{promptId}")]
    public virtual async Task<ListResultDto<AIPromptDto>> GetByPromptAsync(Guid promptId)
    {
        using (_multiTenantDataFilter.Disable())
        {
            var selected = await Repository.GetAsync(promptId);
            EnsureReadAccess(selected);
            var items = await Repository.GetListAsync(v => v.TenantId == selected.TenantId && v.Name == selected.Name);
            var sorted = items.OrderBy(v => v.VersionNumber).ToList();
            return new ListResultDto<AIPromptDto>(
                ObjectMapper.Map<List<AIPrompt>, List<AIPromptDto>>(sorted));
        }
    }

    [HttpGet("{id}")]
    public override async Task<AIPromptDto> GetAsync(Guid id)
    {
        using (_multiTenantDataFilter.Disable())
        {
            var prompt = await Repository.GetAsync(id);
            EnsureReadAccess(prompt);
            return ObjectMapper.Map<AIPrompt, AIPromptDto>(prompt);
        }
    }

    [HttpGet]
    public override async Task<PagedResultDto<AIPromptDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        using (_multiTenantDataFilter.Disable())
        {
            var prompts = await Repository.GetListAsync();
            var accessiblePrompts = _currentTenant.Id is Guid tenantId
                ? prompts.Where(prompt => prompt.TenantId == null || prompt.TenantId == tenantId)
                : prompts;
            var sortedPrompts = accessiblePrompts
                .OrderBy(prompt => prompt.Name)
                .ThenByDescending(prompt => prompt.VersionNumber)
                .ToList();
            var items = sortedPrompts
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToList();

            var inUseIds = GetInUseIds(sortedPrompts);
            var dtos = ObjectMapper.Map<List<AIPrompt>, List<AIPromptDto>>(items);
            foreach (var dto in dtos)
            {
                dto.IsInUse = inUseIds.Contains(dto.Id);
            }

            return new PagedResultDto<AIPromptDto>(sortedPrompts.Count, dtos);
        }
    }

    [HttpPost]
    public override async Task<AIPromptDto> CreateAsync(CreateUpdateAIPromptDto input)
    {
        using (_multiTenantDataFilter.Disable())
        {
            var prompt = await Repository.GetAsync(input.PromptId);
            EnsureReadAccess(prompt);
            var targetTenantId = _currentTenant.Id ?? prompt.TenantId;
            var existingVersion = await Repository.FirstOrDefaultAsync(p =>
                p.TenantId == targetTenantId &&
                p.Name == prompt.Name &&
                p.VersionNumber == input.VersionNumber);
            if (existingVersion != null)
            {
                throw new UserFriendlyException(
                    $"AI prompt '{prompt.Name}' already has version {input.VersionNumber}.");
            }

            var entity = await Repository.InsertAsync(
                new AIPrompt(
                    Guid.CreateVersion7(),
                    prompt.Name,
                    input.VersionNumber,
                    input.SystemPrompt,
                    input.UserPrompt,
                    targetTenantId)
                {
                    MetadataJson = string.IsNullOrWhiteSpace(input.MetadataJson) ? "{}" : input.MetadataJson,
                    IsActive = input.IsActive
                });

            _cacheSignal.Reset();
            return ObjectMapper.Map<AIPrompt, AIPromptDto>(entity);
        }
    }

    [HttpPut("{id}")]
    public override async Task<AIPromptDto> UpdateAsync(Guid id, CreateUpdateAIPromptDto input)
    {
        using (_multiTenantDataFilter.Disable())
        {
            var entity = await Repository.GetAsync(id);
            EnsureMutationAccess(entity);
            var conflictingVersion = await Repository.FirstOrDefaultAsync(p =>
                p.Id != id &&
                p.TenantId == entity.TenantId &&
                p.Name == entity.Name &&
                p.VersionNumber == input.VersionNumber);
            if (conflictingVersion != null)
            {
                throw new UserFriendlyException(
                    $"AI prompt '{entity.Name}' already has version {input.VersionNumber}.");
            }

            entity.VersionNumber = input.VersionNumber;
            entity.SystemPrompt = input.SystemPrompt;
            entity.UserPrompt = input.UserPrompt;
            entity.MetadataJson = string.IsNullOrWhiteSpace(input.MetadataJson) ? "{}" : input.MetadataJson;
            entity.IsActive = input.IsActive;
            entity = await Repository.UpdateAsync(entity);
            _cacheSignal.Reset();
            return ObjectMapper.Map<AIPrompt, AIPromptDto>(entity);
        }
    }

    private void EnsureReadAccess(AIPrompt prompt)
    {
        if (_currentTenant.Id is Guid tenantId && prompt.TenantId is not null && prompt.TenantId != tenantId)
        {
            throw new AbpAuthorizationException("The selected AI prompt belongs to another tenant.");
        }
    }

    /// <summary>
    /// The version the runtime picks for each tenant (or host) and prompt name:
    /// the highest active version (see OpenAIConfigurationResolver.ResolvePromptAsync).
    /// </summary>
    private static HashSet<Guid> GetInUseIds(IEnumerable<AIPrompt> prompts)
    {
        return prompts
            .Where(prompt => prompt.IsActive)
            .GroupBy(prompt => (prompt.TenantId, prompt.Name))
            .Select(group => group.OrderByDescending(prompt => prompt.VersionNumber).First().Id)
            .ToHashSet();
    }

    private void EnsureMutationAccess(AIPrompt prompt)
    {
        if (_currentTenant.Id is Guid tenantId && prompt.TenantId != tenantId)
        {
            throw new AbpAuthorizationException("The selected AI prompt cannot be modified from this tenant.");
        }
    }

    [HttpDelete("{id}")]
    public override async Task DeleteAsync(Guid id)
    {
        using (_multiTenantDataFilter.Disable())
        {
            var entity = await Repository.GetAsync(id);
            EnsureMutationAccess(entity);

            var family = await Repository.GetListAsync(p => p.TenantId == entity.TenantId && p.Name == entity.Name);
            if (GetInUseIds(family).Contains(entity.Id))
            {
                throw new BusinessException(AIErrorCodes.PromptVersionInUse)
                    .WithData("name", entity.Name)
                    .WithData("version", entity.VersionNumber);
            }

            await base.DeleteAsync(id);
            _cacheSignal.Reset();
        }
    }
}
