using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Unity.AI.Prompts;

[Authorize(IdentityConsts.ITOperationsPolicyName)]
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

    public override async Task<AIPromptDto> GetAsync(Guid id)
    {
        using (CurrentTenant.Change(null))
        {
            return await base.GetAsync(id);
        }
    }

    public override async Task<PagedResultDto<AIPromptDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        using (CurrentTenant.Change(null))
        {
            return await base.GetListAsync(input);
        }
    }

    public override async Task<AIPromptDto> CreateAsync(CreateUpdateAIPromptDto input)
    {
        using (CurrentTenant.Change(null))
        {
            return await base.CreateAsync(input);
        }
    }

    public override async Task<AIPromptDto> UpdateAsync(Guid id, CreateUpdateAIPromptDto input)
    {
        using (CurrentTenant.Change(null))
        {
            return await base.UpdateAsync(id, input);
        }
    }

    public override async Task DeleteAsync(Guid id)
    {
        using (CurrentTenant.Change(null))
        {
            await base.DeleteAsync(id);
        }
    }
}
