using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Intakes;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Dashboard;

[Authorize]
public class DashboardAppService : ApplicationService, IDashboardAppService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationStatusRepository _applicationStatusRepository;
    private readonly IApplicantRepository _applicantRepository;
    private readonly IApplicationTagsRepository _applicationTagsRepository;
    private readonly IApplicationFormRepository _applicationFormRepository;
    private readonly IIntakeRepository _intakeRepository;
    private static readonly string[] emptySubStatus = [""];

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

    public virtual async Task<List<GetEconomicRegionDto>> GetEconomicRegionCountAsync(Guid[] intakeIds, string[] categories, string[] statusCodes, string?[] substatus)
    {
        var categoryList = categories.Select(category => category == DashboardConsts.EmptyValue ? null : category).ToArray();
        var statusCodesEnum = statusCodes.Select(s => Enum.Parse<GrantApplicationState>(s));
        // some SubStatus is either null or empty string
        if (substatus.Contains(DashboardConsts.EmptyValue))
        {
            substatus = substatus.Select(sb => sb == DashboardConsts.EmptyValue ? null : sb).Concat(emptySubStatus).ToArray();
        }
        var query = from intake in await _intakeRepository.GetQueryableAsync()
                    join form in await _applicationFormRepository.GetQueryableAsync() on intake.Id equals form.IntakeId
                    join application in await _applicationRepository.GetQueryableAsync() on form.Id equals application.ApplicationFormId
                    join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                    where intakeIds.Contains(intake.Id) && categoryList.Contains(form.Category) && statusCodesEnum.Contains(appStatus.StatusCode) && substatus.Contains(application.SubStatus)
                    select application;

        var result = query?.GroupBy(app => app.EconomicRegion)
            .Select(group => new { EconomicRegion = string.IsNullOrEmpty(group.Key) ? DashboardConsts.EmptyValue : group.Key, Count = group.Count() })
            .GroupBy(app => app.EconomicRegion)
            .Select(group => new GetEconomicRegionDto { EconomicRegion = group.Key, Count = group.Sum(obj=>obj.Count) })
            .OrderBy(o => o.EconomicRegion);

        if (result == null) return [];

        var queryResult = result.ToList();

        return queryResult;
    }

    public virtual async Task<List<GetSectorDto>> GetSectorCountAsync(Guid[] intakeIds, string[] categories, string[] statusCodes, string?[] substatus)
    {
        var categoryList = categories.Select(category => category == DashboardConsts.EmptyValue ? null : category).ToArray();
        var statusCodesEnum = statusCodes.Select(s => Enum.Parse<GrantApplicationState>(s));
        // some SubStatus is either null or empty string
        if (substatus.Contains(DashboardConsts.EmptyValue))
        {
            substatus = substatus.Select(sb => sb == DashboardConsts.EmptyValue ? null : sb).Concat(emptySubStatus).ToArray();
        }
        var query = from intake in await _intakeRepository.GetQueryableAsync()
                    join form in await _applicationFormRepository.GetQueryableAsync() on intake.Id equals form.IntakeId
                    join application in await _applicationRepository.GetQueryableAsync() on form.Id equals application.ApplicationFormId
                    join applicant in await _applicantRepository.GetQueryableAsync() on application.ApplicantId equals applicant.Id
                    join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                    where intakeIds.Contains(intake.Id) && categoryList.Contains(form.Category) && statusCodesEnum.Contains(appStatus.StatusCode) && substatus.Contains(application.SubStatus)
                    select new { application, applicant };

        var result = query?.GroupBy(app => app.applicant.Sector)
            .Select(group => new { Sector = string.IsNullOrEmpty(group.Key) ? DashboardConsts.EmptyValue : group.Key, Count = group.Count() })
            .GroupBy(group => group.Sector)
            .Select(group => new GetSectorDto { Sector = group.Key, Count = group.Sum(obj => obj.Count) })
            .OrderBy(o => o.Sector);

        if (result == null) return [];

        var queryResult = result.ToList();

        return queryResult;
    }

    public virtual async Task<List<GetApplicationStatusDto>> GetApplicationStatusCountAsync(Guid[] intakeIds, string[] categories, string[] statusCodes, string?[] substatus)
    {
        var categoryList = categories.Select(category => category == DashboardConsts.EmptyValue ? null : category).ToArray();
        var statusCodesEnum = statusCodes.Select(s => Enum.Parse<GrantApplicationState>(s));
        // some SubStatus is either null or empty string
        if (substatus.Contains(DashboardConsts.EmptyValue))
        {
            substatus = substatus.Select(sb => sb == DashboardConsts.EmptyValue ? null : sb).Concat(emptySubStatus).ToArray();
        }
        var query = from intake in await _intakeRepository.GetQueryableAsync()
                    join form in await _applicationFormRepository.GetQueryableAsync() on intake.Id equals form.IntakeId
                    join application in await _applicationRepository.GetQueryableAsync() on form.Id equals application.ApplicationFormId
                    join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                    where intakeIds.Contains(intake.Id) && categoryList.Contains(form.Category) && statusCodesEnum.Contains(appStatus.StatusCode) && substatus.Contains(application.SubStatus)
                    select new { application, appStatus };

        var result = query?.GroupBy(app => app.appStatus.InternalStatus)
            .Select(group => new { ApplicationStatus = string.IsNullOrEmpty(group.Key) ? DashboardConsts.EmptyValue : group.Key, Count = group.Count() })
            .GroupBy(app => app.ApplicationStatus)
            .Select(group => new GetApplicationStatusDto { ApplicationStatus = group.Key, Count = group.Sum(obj => obj.Count) })
            .OrderBy(o => o.ApplicationStatus);

        if (result == null) return [];

        var queryResult = result.ToList();

        return queryResult;
    }

    public virtual async Task<List<GetApplicationTagDto>> GetApplicationTagsCountAsync(Guid[] intakeIds, string[] categories, string[] statusCodes, string?[] substatus)
    {
        var categoryList = categories.Select(category => category == DashboardConsts.EmptyValue ? null : category).ToArray();
        var statusCodesEnum = statusCodes.Select(s => Enum.Parse<GrantApplicationState>(s));
        // some SubStatus is either null or empty string
        if (substatus.Contains(DashboardConsts.EmptyValue))
        {
            substatus = substatus.Select(sb => sb == DashboardConsts.EmptyValue ? null : sb).Concat(emptySubStatus).ToArray();
        }
        var query = from intake in await _intakeRepository.GetQueryableAsync()
                    join form in await _applicationFormRepository.GetQueryableAsync() on intake.Id equals form.IntakeId
                    join application in await _applicationRepository.GetQueryableAsync() on form.Id equals application.ApplicationFormId
                    join tag in await _applicationTagsRepository.GetQueryableAsync() on application.Id equals tag.ApplicationId
                    join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                    where intakeIds.Contains(intake.Id) && categoryList.Contains(form.Category) && statusCodesEnum.Contains(appStatus.StatusCode) && substatus.Contains(application.SubStatus)
                    select tag;

        var applicationTags = query.ToList();
        List<string> concatenatedTags = applicationTags.Select(tags => tags.Text).ToList();
        List<string> tags = [];
        concatenatedTags.ForEach(txt => tags.AddRange([.. txt.Split(',')]));
        tags = tags.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        var uniqueTags = new HashSet<string>(tags);
        var result = uniqueTags.Select(tag => new GetApplicationTagDto { ApplicationTag = tag, Count = tags.Count(tg => tg == tag) }).ToList();
        return result;
    }

    public virtual async Task<List<GetSubsectorRequestedAmtDto>> GetRequestedAmountPerSubsectorAsync(Guid[] intakeIds, string[] categories, string[] statusCodes, string?[] substatus)
    {
        var categoryList = categories.Select(category => category == DashboardConsts.EmptyValue ? null : category).ToArray();
        var statusCodesEnum = statusCodes.Select(s => Enum.Parse<GrantApplicationState>(s));
        // some SubStatus is either null or empty string
        if (substatus.Contains(DashboardConsts.EmptyValue))
        {
            substatus = substatus.Select(sb => sb == DashboardConsts.EmptyValue ? null : sb).Concat(emptySubStatus).ToArray();
        }
        var query = from intake in await _intakeRepository.GetQueryableAsync()
                    join form in await _applicationFormRepository.GetQueryableAsync() on intake.Id equals form.IntakeId
                    join application in await _applicationRepository.GetQueryableAsync() on form.Id equals application.ApplicationFormId
                    join applicant in await _applicantRepository.GetQueryableAsync() on application.ApplicantId equals applicant.Id
                    join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                    where intakeIds.Contains(intake.Id) && categoryList.Contains(form.Category) && statusCodesEnum.Contains(appStatus.StatusCode) && substatus.Contains(application.SubStatus)
                    select new { application, applicant };

        var result = query?.GroupBy(app => app.applicant.SubSector)
            .Select(group => new { Subsector = string.IsNullOrEmpty(group.Key) ? DashboardConsts.EmptyValue : group.Key, TotalRequestedAmount = group.Sum(item => item.application.RequestedAmount) })
            .GroupBy(app => app.Subsector)
            .Select(group => new GetSubsectorRequestedAmtDto { Subsector = group.Key, TotalRequestedAmount = group.Sum(obj => obj.TotalRequestedAmount) })
            .OrderBy(o => o.Subsector);

        if (result == null) return [];

        var queryResult = result.ToList();
        queryResult.RemoveAll(item => item.TotalRequestedAmount == 0);
        return queryResult;
    }
}
