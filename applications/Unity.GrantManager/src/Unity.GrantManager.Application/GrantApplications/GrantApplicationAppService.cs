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
using AutoMapper;

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
        private readonly IApplicationStatusRepository _applicationStatusRepository;

        public GrantApplicationAppService(IRepository<GrantApplication, Guid> repository, IApplicationRepository applicationRepository, IApplicationStatusRepository applicationStatusRepository)
             : base(repository)
        {
            _applicationRepository = applicationRepository;
            _applicationStatusRepository = applicationStatusRepository; 
        }

        public override async Task<PagedResultDto<GrantApplicationDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {

            var applications = await _applicationRepository.GetListAsync(
                input.SkipCount,
                input.MaxResultCount,
                input.Sorting
            );           

            var totalCount = await _applicationRepository.CountAsync();

            var mapperConfig = new MapperConfiguration(cfg => {
                cfg.CreateMap<Application, GrantApplicationDto>();                
            });

            var mapper = mapperConfig.CreateMapper();
            var destinations = mapper.Map<List<GrantApplicationDto>>(applications);                      

            return new PagedResultDto<GrantApplicationDto>(
                totalCount,destinations
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



