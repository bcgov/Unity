using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integrations;

/// <summary>
/// Represents a collection of functions to interact with the API endpoints
/// </summary>

public interface IEndpointManagementAppService : ICrudAppService<
            DynamicUrlDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateDynamicUrlDto>

{
    
}
