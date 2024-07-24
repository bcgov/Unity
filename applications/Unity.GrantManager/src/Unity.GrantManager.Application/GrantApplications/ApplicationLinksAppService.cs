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
        var applicationLinksTask = _applicationLinksRepository.GetQueryableAsync();
        var applicationsTask = _applicationRepository.GetQueryableAsync();
        var applicationFormsTask = _applicationFormRepository.GetQueryableAsync();

        await Task.WhenAll(applicationLinksTask, applicationsTask, applicationFormsTask);

        var applicationLinks = await applicationLinksTask;
        var applications = await applicationsTask;
        var applicationForms = await applicationFormsTask;

        var query1 = from applicationLink in applicationLinks
                     join application in applications on applicationLink.LinkedApplicationId equals application.Id
                     join appForm in applicationForms on application.ApplicationFormId equals appForm.Id
                     where applicationLink.ApplicationId == applicationId
                     select new ApplicationLinksInfoDto
                     {
                         Id = applicationLink.Id,
                         ApplicationId = application.Id,
                         ApplicationStatus = application.ApplicationStatus.InternalStatus,
                         ReferenceNumber = application.ReferenceNo,
                         Category = appForm.Category!,
                         ProjectName = application.ProjectName
                     };

        var query2 = from applicationLink in applicationLinks
                     join application in applications on applicationLink.ApplicationId equals application.Id
                     join appForm in applicationForms on application.ApplicationFormId equals appForm.Id
                     where applicationLink.LinkedApplicationId == applicationId
                     select new ApplicationLinksInfoDto
                     {
                         Id = applicationLink.Id,
                         ApplicationId = application.Id,
                         ApplicationStatus = application.ApplicationStatus.InternalStatus,
                         ReferenceNumber = application.ReferenceNo,
                         Category = appForm.Category!,
                         ProjectName = application.ProjectName
                     };

        var combinedQuery = query1.Concat(query2);
        return combinedQuery.ToList();
    }
}
