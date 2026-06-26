using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
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
            return await base.CreateAsync(input);
        }
    }

    [HttpPut("{id}")]
    public override async Task<AIPromptDto> UpdateAsync(Guid id, CreateUpdateAIPromptDto input)
    {
        using (_multiTenantDataFilter.Disable())
        {
            return await base.UpdateAsync(id, input);
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
