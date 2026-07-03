using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Data;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp;

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

    public AIPromptAppService(
        IRepository<AIPrompt, Guid> repository,
        IDataFilter<IMultiTenant> multiTenantDataFilter)
        : base(repository)
    {
        _multiTenantDataFilter = multiTenantDataFilter;
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
            return await base.GetAsync(id);
        }
    }

    [HttpGet]
    public override async Task<PagedResultDto<AIPromptDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        using (_multiTenantDataFilter.Disable())
        {
            return await base.GetListAsync(input);
        }
    }

    [HttpPost]
    public override async Task<AIPromptDto> CreateAsync(CreateUpdateAIPromptDto input)
    {
        using (_multiTenantDataFilter.Disable())
        {
            var prompt = await Repository.GetAsync(input.PromptId);
            var existingVersion = await Repository.FirstOrDefaultAsync(p =>
                p.TenantId == prompt.TenantId &&
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
                    prompt.TenantId)
                {
                    MetadataJson = string.IsNullOrWhiteSpace(input.MetadataJson) ? "{}" : input.MetadataJson,
                    IsActive = input.IsActive
                });

            return ObjectMapper.Map<AIPrompt, AIPromptDto>(entity);
        }
    }

    [HttpPut("{id}")]
    public override async Task<AIPromptDto> UpdateAsync(Guid id, CreateUpdateAIPromptDto input)
    {
        using (_multiTenantDataFilter.Disable())
        {
            var entity = await Repository.GetAsync(id);
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
            return ObjectMapper.Map<AIPrompt, AIPromptDto>(entity);
        }
    }

    [HttpDelete("{id}")]
    public override async Task DeleteAsync(Guid id)
    {
        using (_multiTenantDataFilter.Disable())
        {
            await base.DeleteAsync(id);
        }
    }
}
