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

        var combinedQuery = from applicationLink in applicationLinks
                            join application in applications on applicationLink.ApplicationId equals application.Id into appGroup1
                            from application in appGroup1.DefaultIfEmpty()
                            join application2 in applications on applicationLink.LinkedApplicationId equals application2.Id into appGroup2
                            from application2 in appGroup2.DefaultIfEmpty()
                            join appForm in applicationForms on application.ApplicationFormId equals appForm.Id into appFormGroup
                            from appForm in appFormGroup.DefaultIfEmpty()
                            join appForm2 in applicationForms on application2.ApplicationFormId equals appForm2.Id into appFormGroup2
                            from appForm2 in appFormGroup2.DefaultIfEmpty()
                            where (applicationLink.ApplicationId == applicationId && application != null)
                               || (applicationLink.LinkedApplicationId == applicationId && application2 != null)
                            select new ApplicationLinksInfoDto
                            {
                                Id = applicationLink.Id,
                                ApplicationId = application != null ? application.Id : application2.Id,
                                ApplicationStatus = application != null ? application.ApplicationStatus.InternalStatus : application2.ApplicationStatus.InternalStatus,
                                ReferenceNumber = application != null ? application.ReferenceNo : application2.ReferenceNo,
                                Category = application != null ? appForm.Category : appForm2.Category,
                                ProjectName = application != null ? application.ProjectName : application2.ProjectName
                            };

        return combinedQuery.ToList();
    }
}
