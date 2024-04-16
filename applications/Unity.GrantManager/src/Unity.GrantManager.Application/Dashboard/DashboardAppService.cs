using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Dashboard;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(DashboardAppService), typeof(IDashboardAppService))]
public class DashboardAppService : ApplicationService, IDashboardAppService
{

    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationStatusRepository _applicationStatusRepository;
    private readonly IApplicantRepository _applicantRepository;
    private readonly IApplicationTagsRepository _applicationTagsRepository;
    private readonly IApplicationFormRepository _applicationFormRepository;
    private readonly IIntakeRepository _intakeRepository;

    public DashboardAppService(IApplicationRepository applicationRepository,
        IApplicationStatusRepository applicationStatusRepository,
        IApplicantRepository applicantRepository,
        IApplicationTagsRepository applicationTagsRepository,
        IApplicationFormRepository applicationFormRepository,
        IIntakeRepository intakeRepository
        )
         : base()
    {
        _applicationRepository = applicationRepository;
        _applicationStatusRepository = applicationStatusRepository;
        _applicantRepository = applicantRepository;
        _applicationTagsRepository = applicationTagsRepository;
        _applicationFormRepository = applicationFormRepository;
        _intakeRepository = intakeRepository;
    }

    public async Task<List<GetEconomicRegionDto>> GetEconomicRegionCountAsync(Guid intakeId, string? category)
    {
        if (category == "None")
        {
            category = null;
        }

        var query = from intake in await _intakeRepository.GetQueryableAsync()
                    join form in await _applicationFormRepository.GetQueryableAsync() on intake.Id equals form.IntakeId
                    join application in await _applicationRepository.GetQueryableAsync() on form.Id equals application.ApplicationFormId
                    where intake.Id == intakeId && form.Category == category
                    select application;

        var result = query?.GroupBy(app => app.EconomicRegion).Select(group => new GetEconomicRegionDto { EconomicRegion = string.IsNullOrEmpty(group.Key) ? "None" : group.Key, Count = group.Count() }).OrderBy(o => o.EconomicRegion);
        if (result == null) return [];
        var queryResult = await AsyncExecuter.ToListAsync(result);
        return queryResult;
    }

    public async Task<List<GetSectorDto>> GetSectorCountAsync(Guid intakeId, string? category)
    {
        if (category == "None")
        {
            category = null;
        }

        var query = from intake in await _intakeRepository.GetQueryableAsync()
                    join form in await _applicationFormRepository.GetQueryableAsync() on intake.Id equals form.IntakeId
                    join application in await _applicationRepository.GetQueryableAsync() on form.Id equals application.ApplicationFormId
                    join applicant in await _applicantRepository.GetQueryableAsync() on application.ApplicantId equals applicant.Id
                    where intake.Id == intakeId && form.Category == category
                    select new { application, applicant };

        var result = query?.GroupBy(app => app.applicant.Sector).Select(group => new GetSectorDto { Sector = string.IsNullOrEmpty(group.Key) ? "None" : group.Key, Count = group.Count() }).OrderBy(o => o.Sector);
        if (result == null) return new List<GetSectorDto>();
        var queryResult = await AsyncExecuter.ToListAsync(result);
        return queryResult;
    }

    public async Task<List<GetApplicationStatusDto>> GetApplicationStatusCountAsync(Guid intakeId, string? category)
    {
        if (category == "None")
        {
            category = null;
        }

        var query = from intake in await _intakeRepository.GetQueryableAsync()
                    join form in await _applicationFormRepository.GetQueryableAsync() on intake.Id equals form.IntakeId
                    join application in await _applicationRepository.GetQueryableAsync() on form.Id equals application.ApplicationFormId
                    join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                    where intake.Id == intakeId && form.Category == category
                    select new { application, appStatus };
    
        var result = query?.GroupBy(app => app.appStatus.InternalStatus).Select(group => new GetApplicationStatusDto { ApplicationStatus = string.IsNullOrEmpty(group.Key) ? "None" : group.Key, Count = group.Count() }).OrderBy(o => o.ApplicationStatus);
        if (result == null) return new List<GetApplicationStatusDto>();
        var queryResult = await AsyncExecuter.ToListAsync(result);
        return queryResult;
    }

    public async Task<List<GetApplicationTagDto>> GetApplicationTagsCountAsync()
    {
        var applicationTags = await _applicationTagsRepository.GetListAsync();
        List<string> concatenatedTags = applicationTags.Select(tags => tags.Text).ToList();
        List<string> tags = new List<string>();
        concatenatedTags.ForEach(txt => tags.AddRange(txt.Split(',').ToList()));
        tags = tags.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        var uniqueTags = new HashSet<string>(tags);
        var result = uniqueTags.Select(tag => new GetApplicationTagDto { ApplicationTag = tag, Count= tags.Count(tg => tg==tag)}).ToList();
        return result;
    }
}
