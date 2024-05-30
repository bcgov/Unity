using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Intakes;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Dashboard;

[Authorize]
public class DashboardAppService : ApplicationService, IDashboardAppService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationStatusRepository _applicationStatusRepository;
    private readonly IApplicantRepository _applicantRepository;
    private readonly IApplicationTagsRepository _applicationTagsRepository;
    private readonly IApplicationFormRepository _applicationFormRepository;
    private readonly IApplicationAssignmentRepository _applicationAssignmentRepository;
    private readonly IIntakeRepository _intakeRepository;

    public DashboardAppService(IApplicationRepository applicationRepository,
        IApplicationStatusRepository applicationStatusRepository,
        IApplicantRepository applicantRepository,
        IApplicationTagsRepository applicationTagsRepository,
        IApplicationFormRepository applicationFormRepository,
        IApplicationAssignmentRepository applicationAssignmentRepository,
        IIntakeRepository intakeRepository
       )
         : base()
    {
        _applicationRepository = applicationRepository;
        _applicationStatusRepository = applicationStatusRepository;
        _applicantRepository = applicantRepository;
        _applicationTagsRepository = applicationTagsRepository;
        _applicationFormRepository = applicationFormRepository;
        _applicationAssignmentRepository = applicationAssignmentRepository;
        _intakeRepository = intakeRepository;
    }

    public virtual async Task<List<GetEconomicRegionDto>> GetEconomicRegionCountAsync(DashboardParametersDto dashboardParams)
    {
        var parameters = PrepareParameters(dashboardParams);

        var economicRegionDto = await ExecuteWithDisabledTracking(async () => {

            var query = await GetBaseQueryAsync(parameters);
            var applicationTags = await GetFilteredApplicationTags(query.Select(app => app), parameters);
            query = query.Where(application => applicationTags.Contains(application.Id));

            var result = query.Distinct().GroupBy(app => app.EconomicRegion)
                .Select(group => new { EconomicRegion = string.IsNullOrEmpty(group.Key) ? DashboardConsts.EmptyValue : group.Key, Count = group.Count() })
                .GroupBy(app => app.EconomicRegion)
                .Select(group => new GetEconomicRegionDto { EconomicRegion = group.Key, Count = group.Sum(obj => obj.Count) })
                .OrderBy(o => o.EconomicRegion);

            return result.ToList();
        });

        return economicRegionDto;
    }

    public virtual async Task<List<GetSectorDto>> GetSectorCountAsync(DashboardParametersDto dashboardParams)
    {
        var parameters = PrepareParameters(dashboardParams);

        var sectorCountDto = await ExecuteWithDisabledTracking(async () => {

            var query = from application in await GetBaseQueryAsync(parameters)
                        join applicant in await _applicantRepository.GetQueryableAsync() on application.ApplicantId equals applicant.Id
                        select new { application, applicant };

            var applicationTags = await GetFilteredApplicationTags(query.Select(app => app.application), parameters);
            query = query.Where(application => applicationTags.Contains(application.application.Id));

            var result = query.Distinct().GroupBy(app => app.applicant.Sector)
                .Select(group => new { Sector = string.IsNullOrEmpty(group.Key) ? DashboardConsts.EmptyValue : group.Key, Count = group.Count() })
                .GroupBy(group => group.Sector)
                .Select(group => new GetSectorDto { Sector = group.Key, Count = group.Sum(obj => obj.Count) })
                .OrderBy(o => o.Sector);

            return result.ToList();
        });

        return sectorCountDto;
    }

    public virtual async Task<List<GetApplicationStatusDto>> GetApplicationStatusCountAsync(DashboardParametersDto dashboardParams)
    {
        var parameters = PrepareParameters(dashboardParams);

        var applicationStatusDto = await ExecuteWithDisabledTracking(async () => {

            var query = from application in await GetBaseQueryAsync(parameters)
                        join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                        select new { application, appStatus };

            var applicationTags = await GetFilteredApplicationTags(query.Select(app => app.application), parameters);
            query = query.Where(application => applicationTags.Contains(application.application.Id));

            var result = query.Distinct().GroupBy(app => app.appStatus.InternalStatus)
                .Select(group => new { ApplicationStatus = string.IsNullOrEmpty(group.Key) ? DashboardConsts.EmptyValue : group.Key, Count = group.Count() })
                .GroupBy(app => app.ApplicationStatus)
                .Select(group => new GetApplicationStatusDto { ApplicationStatus = group.Key, Count = group.Sum(obj => obj.Count) })
                .OrderBy(o => o.ApplicationStatus);

            return result.ToList();
        });

        return applicationStatusDto;
    }

    public virtual async Task<List<GetApplicationTagDto>> GetApplicationTagsCountAsync(DashboardParametersDto dashboardParams)
    {
        var parameters = PrepareParameters(dashboardParams);

        var applicationTagsDto = await ExecuteWithDisabledTracking(async () => {

            var query = from application in await GetBaseQueryAsync(parameters)
                        join tag in await _applicationTagsRepository.GetQueryableAsync() on application.Id equals tag.ApplicationId
                        select tag;

            var applicationTags = query.Distinct().ToList();
            List<string> concatenatedTags = applicationTags.Select(tags => tags.Text).ToList();
            List<string> tags = [];
            concatenatedTags.ForEach(txt => tags.AddRange([.. txt.Split(',')]));
            tags = tags.Where(s => !string.IsNullOrWhiteSpace(s) && parameters.Tags.Contains(s)).ToList();
            var uniqueTags = new HashSet<string>(tags);
            var result = uniqueTags.Select(tag => new GetApplicationTagDto { ApplicationTag = tag, Count = tags.Count(tg => tg == tag) }).ToList();
            return result;
        });

        return applicationTagsDto;
    }

    public virtual async Task<List<GetSubsectorRequestedAmtDto>> GetRequestedAmountPerSubsectorAsync(DashboardParametersDto dashboardParams)
    {
        var parameters = PrepareParameters(dashboardParams);

        var subSectorRequestedAmtDto = await ExecuteWithDisabledTracking(async () => {

            var query = from application in await GetBaseQueryAsync(parameters)
                        join applicant in await _applicantRepository.GetQueryableAsync() on application.ApplicantId equals applicant.Id
                        select new { application, applicant };

            var applicationTags = await GetFilteredApplicationTags(query.Select(app => app.application), parameters);
            query = query.Where(application => applicationTags.Contains(application.application.Id));

            var result = query.Distinct().GroupBy(app => app.applicant.SubSector)
                .Select(group => new { Subsector = string.IsNullOrEmpty(group.Key) ? DashboardConsts.EmptyValue : group.Key, TotalRequestedAmount = group.Sum(item => item.application.RequestedAmount) })
                .GroupBy(app => app.Subsector)
                .Select(group => new GetSubsectorRequestedAmtDto { Subsector = group.Key, TotalRequestedAmount = group.Sum(obj => obj.TotalRequestedAmount) })
                .OrderBy(o => o.Subsector);

            var queryResult = result.ToList();
            queryResult.RemoveAll(item => item.TotalRequestedAmount == 0);

            return queryResult;
        });

        return subSectorRequestedAmtDto;
    }

    private async Task<List<Guid>> GetFilteredApplicationTags(IQueryable<Application> applications, DashboardParameters parameters)
    {
        var tags = await _applicationTagsRepository.GetQueryableAsync();
        var tagsResult = tags.Join(applications, tag => tag.ApplicationId, app => app.Id, (tag, app) => tag);

        var applicationIdsWithTags = tagsResult.AsEnumerable()
            .SelectMany(tag => tag.Text.Split(','), (tagResult, tag) => new { tagResult.ApplicationId, Tag = tag })
            .Where(tag => parameters.Tags.Contains(tag.Tag))
            .Select(tag => tag.ApplicationId)
            .ToHashSet();

        if (parameters.Tags.Contains(string.Empty))
        {
            var applicationsWithoutTags = applications.Where(app => !tagsResult.Any(res => res.ApplicationId == app.Id));
            applications = applications.Where(app => applicationIdsWithTags.Contains(app.Id)).Union(applicationsWithoutTags);
        }
        else
        {
            applications = applications.Where(app => applicationIdsWithTags.Contains(app.Id));
        }

        return await applications.Select(app => app.Id).ToListAsync();
    }

    private async Task<IQueryable<Application>> GetBaseQueryAsync(DashboardParameters parameters)
    {
        var query = from intake in await _intakeRepository.GetQueryableAsync()
                    join form in await _applicationFormRepository.GetQueryableAsync() on intake.Id equals form.IntakeId
                    join application in await _applicationRepository.GetQueryableAsync() on form.Id equals application.ApplicationFormId
                    join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                    join assignee in await _applicationAssignmentRepository.GetQueryableAsync() on application.Id equals assignee.ApplicationId into appAssignees
                    from subAssignee in appAssignees.DefaultIfEmpty()
                    where parameters.IntakeIds.Contains(intake.Id)
                        && parameters.Categories.Contains(form.Category)
                        && parameters.StatusCodes.Contains(appStatus.StatusCode)
                        && parameters.SubStatuses.Contains(application.SubStatus)
                        && (parameters.DateFrom == null || application.SubmissionDate >= parameters.DateFrom)
                        && (parameters.DateTo == null || application.SubmissionDate <= parameters.DateTo)
                        && (parameters.Assignees.Contains(subAssignee.AssigneeId.ToString())
                           || (subAssignee == null && parameters.Assignees.Contains(null)))
                    select application;

        return query;
    }

    private async Task<TResult> ExecuteWithDisabledTracking<TResult>(Func<Task<TResult>> logic)
    {
        using (_intakeRepository.DisableTracking())
        using (_applicationFormRepository.DisableTracking())
        using (_applicationRepository.DisableTracking())
        using (_applicationTagsRepository.DisableTracking())
        using (_applicationStatusRepository.DisableTracking())
        {
            return await logic();
        }
    }

    internal virtual DashboardParameters PrepareParameters(DashboardParametersDto dashboardParams)
    {
        var parameters = new DashboardParameters
        {
            IntakeIds = dashboardParams.IntakeIds.ToArray(),
            Categories = dashboardParams.Categories.Select(category => category == DashboardConsts.EmptyValue ? null : category).ToArray(),
            StatusCodes = dashboardParams.StatusCodes.Select(s => Enum.Parse<GrantApplicationState>(s)),
            DateFrom = dashboardParams.DateFrom,
            DateTo = dashboardParams.DateTo?.AddDays(1).AddTicks(-1),
            Tags = dashboardParams.Tags.Select(tag => tag ?? "").ToArray(),
            Assignees = dashboardParams.Assignees.ToArray()
        };

        // some SubStatus is either null or empty string
        if (dashboardParams.Substatus.Contains(DashboardConsts.EmptyValue))
        {
            string[] emptySubStatus = [""];
            parameters.SubStatuses = dashboardParams.Substatus.Select(sb => sb == DashboardConsts.EmptyValue ? null : sb).Concat(emptySubStatus).ToArray();
        }
        else
        {
            parameters.SubStatuses = dashboardParams.Substatus;
        }

        return parameters;
    }

    internal sealed class DashboardParameters
    {
        public Guid[] IntakeIds { get; set; } = Array.Empty<Guid>();
        public string?[] Categories { get; set; } = [];
        public IEnumerable<GrantApplicationState> StatusCodes { get; set; } = [];
        public string?[] SubStatuses { get; set; } = [];
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string?[] Tags { get; set; } = [];
        public string[] Assignees { get; set; } = [];
    }

    internal sealed class ApplicationTags
    {
        public Guid ApplicationId { get; set; }
        public string Tag { get; set; } = string.Empty;
    }
}
