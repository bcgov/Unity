using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Unity.AI.Prompts;

[Authorize(IdentityConsts.ITOperationsPolicyName)]
[Route("api/app/ai/prompt-versions")]
public class AIPromptVersionAppService :
    CrudAppService<
        AIPromptVersion,
        AIPromptVersionDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateAIPromptVersionDto>,
    IAIPromptVersionAppService
{
    public AIPromptVersionAppService(IRepository<AIPromptVersion, Guid> repository)
        : base(repository)
    {
        GetPolicyName = IdentityConsts.ITOperationsPolicyName;
        GetListPolicyName = IdentityConsts.ITOperationsPolicyName;
        CreatePolicyName = IdentityConsts.ITOperationsPolicyName;
        UpdatePolicyName = IdentityConsts.ITOperationsPolicyName;
        DeletePolicyName = IdentityConsts.ITOperationsPolicyName;
    }

    [HttpGet("by-prompt/{promptId}")]
    public async Task<ListResultDto<AIPromptVersionDto>> GetByPromptAsync(Guid promptId)
    {
        using (CurrentTenant.Change(null))
        {
            var items = await Repository.GetListAsync(v => v.PromptId == promptId);
            var sorted = items.OrderBy(v => v.VersionNumber).ToList();
            return new ListResultDto<AIPromptVersionDto>(
                ObjectMapper.Map<List<AIPromptVersion>, List<AIPromptVersionDto>>(sorted));
        }
    }

    [HttpGet("{id}")]
    public override async Task<AIPromptVersionDto> GetAsync(Guid id)
    {
        using (CurrentTenant.Change(null))
        {
            return await base.GetAsync(id);
        }
    }

    [HttpGet]
    public override async Task<PagedResultDto<AIPromptVersionDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        using (CurrentTenant.Change(null))
        {
            return await base.GetListAsync(input);
        }
    }

    [HttpPost]
    public override async Task<AIPromptVersionDto> CreateAsync(CreateUpdateAIPromptVersionDto input)
    {
        using (CurrentTenant.Change(null))
        {
            return await base.CreateAsync(input);
        }
    }

    [HttpPut("{id}")]
    public override async Task<AIPromptVersionDto> UpdateAsync(Guid id, CreateUpdateAIPromptVersionDto input)
    {
        using (CurrentTenant.Change(null))
        {
            return await base.UpdateAsync(id, input);
        }
    }

    [HttpDelete("{id}")]
    public override async Task DeleteAsync(Guid id)
    {
        using (CurrentTenant.Change(null))
        {
            await base.DeleteAsync(id);
        }
    }
}
