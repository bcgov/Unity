using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.GrantApplications;

[Authorize]
[ExposeServices(typeof(ApplicationLinksAppService), typeof(IApplicationLinksService))]
public class ApplicationLinksAppService : CrudAppService<
            ApplicationLink,
            ApplicationLinksDto,
            Guid>, IApplicationLinksService
{
    private readonly IApplicationLinksRepository _applicationLinksRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationFormRepository _applicationFormRepository;

    public ApplicationLinksAppService(IRepository<ApplicationLink, Guid> repository,
        IApplicationLinksRepository applicationLinksRepository,
        IApplicationFormRepository applicationFormRepository,
        IApplicationRepository applicationRepository) : base(repository)
    {
        _applicationLinksRepository = applicationLinksRepository;
        _applicationRepository = applicationRepository;
        _applicationFormRepository = applicationFormRepository;
    }
    
    public async Task<List<ApplicationLinksInfoDto>> GetListByApplicationAsync(Guid applicationId)
    {
        var query1 = from applicationLinks in await _applicationLinksRepository.GetQueryableAsync()
                    join application in await _applicationRepository.GetQueryableAsync() on applicationLinks.LinkedApplicationId equals application.Id
                    join appForm in await _applicationFormRepository.GetQueryableAsync() on application.ApplicationFormId equals appForm.Id
                    where applicationLinks.ApplicationId == applicationId
                    select new ApplicationLinksInfoDto{
                        Id = applicationLinks.Id,
                        ApplicationId = application.Id,
                        ApplicationStatus = application.ApplicationStatus.InternalStatus,
                        ReferenceNumber = application.ReferenceNo,
                        Category = appForm.Category!,
                        ProjectName = application.ProjectName
                    };
                
        var query2 = from applicationLinks in await _applicationLinksRepository.GetQueryableAsync()
                    join application in await _applicationRepository.GetQueryableAsync() on applicationLinks.ApplicationId equals application.Id
                    join appForm in await _applicationFormRepository.GetQueryableAsync() on application.ApplicationFormId equals appForm.Id
                    where applicationLinks.LinkedApplicationId == applicationId
                    select new ApplicationLinksInfoDto{
                        Id = applicationLinks.Id,
                        ApplicationId = application.Id,
                        ApplicationStatus = application.ApplicationStatus.InternalStatus,
                        ReferenceNumber = application.ReferenceNo,
                        Category = appForm.Category!,
                        ProjectName = application.ProjectName
                    };

        var combinedQuery = query1.Union(query2);

        return combinedQuery.ToList();
    }
}
