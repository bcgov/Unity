using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using Volo.Abp.DependencyInjection;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.GrantApplications
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(GrantApplicationAppService))]
    public class GrantApplicationAppService :
        CrudAppService<
        GrantApplication,
        GrantApplicationDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateGrantApplicationDto>
    {

        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationStatusRepository _applicationStatusRepository;
        private readonly IApplicationUserAssignmentRepository _userAssignmentRepository;

        public GrantApplicationAppService(IRepository<GrantApplication, Guid> repository, IApplicationRepository applicationRepository, IApplicationStatusRepository applicationStatusRepository, IApplicationUserAssignmentRepository userAssignmentRepository)
             : base(repository)
        {
            _applicationRepository = applicationRepository;
            _applicationStatusRepository = applicationStatusRepository;
            _userAssignmentRepository = userAssignmentRepository;
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
                     
           
            //Convert the query result to a list of BookDto objects
            var applicationDtos = queryResult.Select(x =>
            {                
                var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(x.application);
                appDto.Status = x.appStatus.InternalStatus;
                appDto.Assignees = getAssignees(x.application.Id);
                return appDto;
            }).ToList();

            //Get the total count with another query
            var totalCount = await _applicationRepository.GetCountAsync();
            
            return new PagedResultDto<GrantApplicationDto>(
                totalCount,
                applicationDtos
            );            
        }

        public List<GrantApplicationAssigneeDto> getAssignees(Guid applicationId)
        {
            IQueryable<ApplicationUserAssignment> queryableAssignment = _userAssignmentRepository.GetQueryableAsync().Result;
            var assignments = queryableAssignment.Where(a => a.ApplicationId.Equals(applicationId)).ToList();
            
            var assignees = ObjectMapper.Map<List<ApplicationUserAssignment>, List<GrantApplicationAssigneeDto>>(assignments);
            return assignees;
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
                try
                {
                    var application = await _applicationRepository.GetAsync(applicationId);
                    if (application != null)
                    {
                        application.ApplicationStatusId = statusId;
                        await _applicationRepository.UpdateAsync(application);
                    }
                } catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
                
            }
        }

        public async Task AddAssignee(Guid[] applicationIds, string AssigneeKeycloakId, string AssigneeDisplayName)
        {
            foreach (Guid applicationId in applicationIds)
            {
                try
                {
                    var application = await _applicationRepository.GetAsync(applicationId);
                    if (application != null)
                    {
                        await _userAssignmentRepository.InsertAsync(
                            new ApplicationUserAssignment
                            {
                                OidcSub = AssigneeKeycloakId,
                                //ApplicationFormId = appForm1.Id,
                                ApplicationId = application.Id,
                                AssigneeDisplayName = AssigneeDisplayName,
                                AssignmentTime = DateTime.Now
                            }
                            );
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
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



