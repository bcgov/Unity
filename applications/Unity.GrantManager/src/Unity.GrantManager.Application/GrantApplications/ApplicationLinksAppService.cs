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
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicationLinksAppService), typeof(IApplicationLinksService))]
public class ApplicationLinksAppService : CrudAppService<
            ApplicationLinks,
            ApplicationLinksDto,
            Guid>, IApplicationLinksService
{
    private readonly IApplicationLinksRepository _applicationLinksRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationFormRepository _applicationFormRepository;
    private readonly IApplicantRepository _applicantRepository;

    public ApplicationLinksAppService(IRepository<ApplicationLinks, Guid> repository,
        IApplicationLinksRepository applicationLinksRepository,
        IApplicationFormRepository applicationFormRepository,
        IApplicantRepository applicantRepository,
        IApplicationRepository applicationRepository) : base(repository)
    {
        _applicationLinksRepository = applicationLinksRepository;
        _applicationRepository = applicationRepository;
        _applicationFormRepository = applicationFormRepository;
        _applicantRepository = applicantRepository;
    }
    
    public async Task<List<ApplicationLinksInfoDto>> GetListByApplicationAsync(Guid applicationId)
    {
        var query = from applicationLinks in await _applicationLinksRepository.GetQueryableAsync()
                    join application in await _applicationRepository.GetQueryableAsync() on applicationLinks.LinkedApplicationId equals application.Id
                    join appForm in await _applicationFormRepository.GetQueryableAsync() on application.ApplicationFormId equals appForm.Id
                    join applicant in await _applicantRepository.GetQueryableAsync() on application.ApplicantId equals applicant.Id
                    where applicationLinks.ApplicationId == applicationId
                    select new ApplicationLinksInfoDto{
                        Id = applicationLinks.Id,
                        ApplicationId = application.Id,
                        ApplicationStatus = application.ApplicationStatus.InternalStatus,
                        ReferenceNumber = application.ReferenceNo,
                        Category = appForm.Category!,
                        ApplicantName = applicant.ApplicantName
                    };

        return query.ToList();
    }
}
