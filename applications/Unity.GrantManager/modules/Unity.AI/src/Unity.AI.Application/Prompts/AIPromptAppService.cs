using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

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
    public AIPromptAppService(IRepository<AIPrompt, Guid> repository)
        : base(repository)
    {
        GetPolicyName = IdentityConsts.ITOperationsPolicyName;
        GetListPolicyName = IdentityConsts.ITOperationsPolicyName;
        CreatePolicyName = IdentityConsts.ITOperationsPolicyName;
        UpdatePolicyName = IdentityConsts.ITOperationsPolicyName;
        DeletePolicyName = IdentityConsts.ITOperationsPolicyName;
    }

    [HttpGet("{id}")]
    public override async Task<AIPromptDto> GetAsync(Guid id)
    {
        using (CurrentTenant.Change(null))
        {
            return await base.GetAsync(id);
        }
    }

    [HttpGet]
    public override async Task<PagedResultDto<AIPromptDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        using (CurrentTenant.Change(null))
        {
            return await base.GetListAsync(input);
        }
    }

    [HttpPost]
    public override async Task<AIPromptDto> CreateAsync(CreateUpdateAIPromptDto input)
    {
        using (CurrentTenant.Change(null))
        {
            return await base.CreateAsync(input);
        }
    }

    [HttpPut("{id}")]
    public override async Task<AIPromptDto> UpdateAsync(Guid id, CreateUpdateAIPromptDto input)
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
