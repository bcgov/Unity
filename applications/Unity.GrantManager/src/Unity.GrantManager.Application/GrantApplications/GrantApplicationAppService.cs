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
            //Get the IQueryable<Book> from the repository
            var queryable = await _applicationRepository.GetQueryableAsync();
            
            //Prepare a query to join books and authors
            var query = from application in queryable
                        join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                        select new { application, appStatus };
            
            //Paging
            query = query
                .OrderBy(NormalizeSorting(input.Sorting))
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount);
            
            //Execute the query and get a list
            var queryResult = await AsyncExecuter.ToListAsync(query);            

            var mapperConfig = new MapperConfiguration(cfg => {
                cfg.CreateMap<Application, GrantApplicationDto>();
            });
            var mapper = mapperConfig.CreateMapper();

            //Convert the query result to a list of BookDto objects
            var applicationDtos = queryResult.Select(x =>
            {                
                var appDto = mapper.Map<Application, GrantApplicationDto>(x.application);
                appDto.Status = x.appStatus.InternalStatus;               
                return appDto;
            }).ToList();

            //Get the total count with another query
            var totalCount = await _applicationRepository.GetCountAsync();           

            return new PagedResultDto<GrantApplicationDto>(
                totalCount,
                applicationDtos
            );            
        }

        private static string NormalizeSorting(string sorting)
        {
            if (sorting.IsNullOrEmpty())
            {
                return $"application.{nameof(Application.ProjectName)}";
            }

            return $"application.{sorting}";
        }       

        public async Task UpdateApplicationStatus(Guid[] applicationIds, Guid statusId)
        {
            foreach(Guid applicationId in applicationIds)
            {
                var application = await _applicationRepository.GetAsync(applicationId);
                if(application != null)
                {
                    application.ApplicationStatusId = statusId;
                    await _applicationRepository.UpdateAsync(application);
                }
            }
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



