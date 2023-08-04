using System;
using System.Collections.Generic;

using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using System.Linq.Dynamic.Core;
using Unity.GrantManager.Applications;
using Microsoft.AspNetCore.Authorization;



namespace Unity.GrantManager.GrantApplications
{
    public class GrantApplicationAppService :
        CrudAppService<
        GrantApplication,
        GrantApplicationDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateGrantApplicationDto>,
        IGrantApplicationAppService
    {

        private readonly IApplicationRepository _applicationRepository;

        public GrantApplicationAppService(IRepository<GrantApplication, Guid> repository, IApplicationRepository applicationRepository)
             : base(repository)
        {
            _applicationRepository = applicationRepository;
        }

        public override async Task<PagedResultDto<GrantApplicationDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {

            var applications = await _applicationRepository.GetListAsync(
                input.SkipCount,
                input.MaxResultCount,
                input.Sorting
            );

            var totalCount = await _applicationRepository.CountAsync();
                

            return new PagedResultDto<GrantApplicationDto>(
                totalCount,
                ObjectMapper.Map<List<Application>, List<GrantApplicationDto>>(applications)
            );
            
        }      

        
    }

    public static class IQueryableExtensions
    {
        public static IQueryable<T> Sort<T>(this IQueryable<T> query, PagedAndSortedResultRequestDto input)
        {
            if (input is ISortedResultRequest sortInput)
            {
                if (!sortInput.Sorting.IsNullOrWhiteSpace())
                {
                    return query.OrderBy(input.Sorting);
                }
            }

            return query;
        }

        
    }
}



