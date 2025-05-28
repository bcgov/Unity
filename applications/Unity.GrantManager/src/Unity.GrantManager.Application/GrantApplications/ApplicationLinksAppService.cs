using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
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
    public IApplicationLinksRepository ApplicationLinksRepository { get; set; } = null!;
    public IApplicationRepository ApplicationRepository { get; set; } = null!;
    public IApplicationFormRepository ApplicationFormRepository { get; set; } = null!;

    // Constructor for the required repository
    public ApplicationLinksAppService(IRepository<ApplicationLink, Guid> repository) : base(repository) { }

    public async Task<List<ApplicationLinksInfoDto>> GetListByApplicationAsync(Guid applicationId)
    {
        var applicationLinksQuery = await ApplicationLinksRepository.GetQueryableAsync();
        var applicationsQuery = await ApplicationRepository.GetQueryableAsync();
        var applicationFormsQuery = await ApplicationFormRepository.GetQueryableAsync();

        var combinedQuery = from applicationLinks in applicationLinksQuery
                            join application in applicationsQuery on applicationLinks.LinkedApplicationId equals application.Id into appLinks
                            from application in appLinks.DefaultIfEmpty() // Left join for safety
                            join appForm in applicationFormsQuery on application.ApplicationFormId equals appForm.Id into appForms
                            from appForm in appForms.DefaultIfEmpty() // Left join for safety
                            where applicationLinks.ApplicationId == applicationId || applicationLinks.LinkedApplicationId == applicationId
                            select new ApplicationLinksInfoDto
                            {
                                Id = applicationLinks.Id,
                                ApplicationId = application.Id,
                                ApplicationStatus = application.ApplicationStatus.InternalStatus,
                                ReferenceNumber = application.ReferenceNo,
                                Category = appForm.Category ?? "Unknown", // Handle potential nulls
                                ProjectName = application.ProjectName
                            };

        return await combinedQuery.ToListAsync();
    }

    public async Task<ApplicationLinksInfoDto> GetLinkedApplicationAsync(Guid currentApplicationId, Guid linkedApplicationId)
    {
        var applicationLinksQuery = await ApplicationLinksRepository.GetQueryableAsync();
        var applicationsQuery = await ApplicationRepository.GetQueryableAsync();
        var applicationFormsQuery = await ApplicationFormRepository.GetQueryableAsync();

        var combinedQuery = from applicationLinks in applicationLinksQuery
                            join application in applicationsQuery on applicationLinks.LinkedApplicationId equals application.Id into appLinks
                            from application in appLinks.DefaultIfEmpty() // Left join for safety
                            join appForm in applicationFormsQuery on application.ApplicationFormId equals appForm.Id into appForms
                            from appForm in appForms.DefaultIfEmpty() // Left join for safety
                            where applicationLinks.ApplicationId == linkedApplicationId && applicationLinks.LinkedApplicationId == currentApplicationId
                            select new ApplicationLinksInfoDto
                            {
                                Id = applicationLinks.Id,
                                ApplicationId = application.Id,
                                ApplicationStatus = application.ApplicationStatus.InternalStatus,
                                ReferenceNumber = application.ReferenceNo,
                                Category = appForm.Category ?? "Unknown", // Handle potential nulls
                                ProjectName = application.ProjectName
                            };

        return await combinedQuery.SingleAsync();
    }
}