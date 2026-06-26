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

namespace Unity.AI.Prompts;

[Authorize(IdentityConsts.ITOperationsPolicyName)]
[Route("api/app/ai/prompt-versions")]
public class AIPromptVersionAppService :
    CrudAppService<
        AIPrompt,
        AIPromptVersionDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateAIPromptVersionDto>,
    IAIPromptVersionAppService
{
    private readonly IDataFilter<IMultiTenant> _multiTenantDataFilter;

    public AIPromptVersionAppService(
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
    public async Task<ListResultDto<AIPromptVersionDto>> GetByPromptAsync(Guid promptId)
    {
        using (_multiTenantDataFilter.Disable())
        {
            var selected = await Repository.GetAsync(promptId);
            var items = await Repository.GetListAsync(v => v.TenantId == selected.TenantId && v.Name == selected.Name);
            var sorted = items.OrderBy(v => v.VersionNumber).ToList();
            return new ListResultDto<AIPromptVersionDto>(
                ObjectMapper.Map<List<AIPrompt>, List<AIPromptVersionDto>>(sorted));
        }
    }

    [HttpGet("{id}")]
    public override async Task<AIPromptVersionDto> GetAsync(Guid id)
    {
        using (_multiTenantDataFilter.Disable())
        {
            return await base.GetAsync(id);
        }
    }

    [HttpGet]
    public override async Task<PagedResultDto<AIPromptVersionDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        using (_multiTenantDataFilter.Disable())
        {
            return await base.GetListAsync(input);
        }
    }

    [HttpPost]
    public override async Task<AIPromptVersionDto> CreateAsync(CreateUpdateAIPromptVersionDto input)
    {
        using (_multiTenantDataFilter.Disable())
        {
            var prompt = await Repository.GetAsync(input.PromptId);
            var entity = await Repository.InsertAsync(
                new AIPrompt(
                    Guid.CreateVersion7(),
                    prompt.Name,
                    input.VersionNumber,
                    input.SystemPrompt,
                    input.UserPrompt)
                {
                    MetadataJson = input.MetadataJson,
                    IsActive = input.IsActive
                });

            return ObjectMapper.Map<AIPrompt, AIPromptVersionDto>(entity);
        }
    }

    [HttpPut("{id}")]
    public override async Task<AIPromptVersionDto> UpdateAsync(Guid id, CreateUpdateAIPromptVersionDto input)
    {
        using (_multiTenantDataFilter.Disable())
        {
            var entity = await Repository.GetAsync(id);
            entity.VersionNumber = input.VersionNumber;
            entity.SystemPrompt = input.SystemPrompt;
            entity.UserPrompt = input.UserPrompt;
            entity.MetadataJson = input.MetadataJson;
            entity.IsActive = input.IsActive;
            entity = await Repository.UpdateAsync(entity);
            return ObjectMapper.Map<AIPrompt, AIPromptVersionDto>(entity);
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
