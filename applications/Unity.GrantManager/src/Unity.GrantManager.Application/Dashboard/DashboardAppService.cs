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
    private readonly IIntakeRepository _intakeRepository;
    private readonly IApplicationAssignmentRepository _applicationAssignmentRepository;

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
        _intakeRepository = intakeRepository;
        _applicationAssignmentRepository = applicationAssignmentRepository;
    }

    public virtual async Task<List<GetEconomicRegionDto>> GetEconomicRegionCountAsync(DashboardParametersDto dashboardParams)
    {
        var parameters = PrepareParameters(dashboardParams);

        var economicRegionDto = await ExecuteWithDisabledTracking(async () => {

            var query = from baseQuery in await GetBaseQueryAsync(parameters)
                        select new { baseQuery.Application };

            var applicationTags = await GetFilteredApplicationTags(query.Select(app => app.Application), parameters);
            query = query.Where(application => applicationTags.Contains(application.Application.Id));

            var result = query.Distinct().GroupBy(app => app.Application.EconomicRegion)
                .Select(group => new { EconomicRegion = string.IsNullOrEmpty(group.Key) ? DashboardConsts.EmptyValue : group.Key, Count = group.Count() })
                .GroupBy(app => app.EconomicRegion)
                .Select(group => new GetEconomicRegionDto { EconomicRegion = group.Key, Count = group.Sum(obj => obj.Count) })
                .OrderBy(o => o.EconomicRegion);

            return result.ToList();
        });

        return economicRegionDto;
    }

    public virtual async Task<List<GetApplicationStatusDto>> GetApplicationStatusCountAsync(DashboardParametersDto dashboardParams)
    {
        var parameters = PrepareParameters(dashboardParams);

        var applicationStatusDto = await ExecuteWithDisabledTracking(async () => {

            var query = from baseQuery in await GetBaseQueryAsync(parameters)
                        select new { baseQuery.Application, baseQuery.ApplicationStatus };

            var applicationTags = await GetFilteredApplicationTags(query.Select(app => app.Application), parameters);
            query = query.Where(application => applicationTags.Contains(application.Application.Id));

            var result = query.Distinct().GroupBy(app => app.ApplicationStatus.InternalStatus)
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

            var query = from baseQuery in await GetBaseQueryAsync(parameters)
                        join tag in await _applicationTagsRepository.GetQueryableAsync() on baseQuery.Application.Id equals tag.ApplicationId
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

            var query = from baseQuery in await GetBaseQueryAsync(parameters)
                        join applicant in await _applicantRepository.GetQueryableAsync() on baseQuery.Application.ApplicantId equals applicant.Id
                        select new { baseQuery.Application, applicant };

            var applicationTags = await GetFilteredApplicationTags(query.Select(app => app.Application), parameters);
            query = query.Where(application => applicationTags.Contains(application.Application.Id));

            var result = query.Distinct().GroupBy(app => app.applicant.SubSector)
                .Select(group => new { Subsector = string.IsNullOrEmpty(group.Key) ? DashboardConsts.EmptyValue : group.Key, TotalRequestedAmount = group.Sum(item => item.Application.RequestedAmount) })
                .GroupBy(app => app.Subsector)
                .Select(group => new GetSubsectorRequestedAmtDto { Subsector = group.Key, TotalRequestedAmount = group.Sum(obj => obj.TotalRequestedAmount) })
                .OrderBy(o => o.Subsector);

            var queryResult = result.ToList();
            queryResult.RemoveAll(item => item.TotalRequestedAmount == 0);

            return queryResult;
        });

        return subSectorRequestedAmtDto;
    }

    public virtual async Task<List<GetApplicationAssigneeDto>> GetApplicationAssigneeCountAsync(DashboardParametersDto dashboardParams)
    {
        var parameters = PrepareParameters(dashboardParams);

        var applicationAssigneeDto = await ExecuteWithDisabledTracking(async () => {

            var query = from baseQuery in await GetBaseQueryAsync(parameters)
                        where parameters.Assignees.Contains(baseQuery.AppAssignee.AssigneeId.ToString())
                        select new { baseQuery.Application, baseQuery.AppAssignee };

            var applicationTags = await GetFilteredApplicationTags(query.Select(app => app.Application), parameters);
            query = query.Where(application => applicationTags.Contains(application.Application.Id));

            var result = query.GroupBy(app => app.AppAssignee.Assignee)
                .Select(group => new GetApplicationAssigneeDto
                {
                    ApplicationAssignee = string.IsNullOrEmpty(group.Key!.FullName) ? DashboardConsts.EmptyValue : group.Key!.FullName,
                    AssigneeOidcDisplayName = string.IsNullOrEmpty(group.Key!.OidcDisplayName) ? DashboardConsts.EmptyValue : group.Key!.OidcDisplayName,
                    Count = group.Count()
                })
                .OrderBy(o => o.ApplicationAssignee);

            var queryResult = result.ToList();

            return queryResult;
        });

        return applicationAssigneeDto;
    }

    public virtual async Task<List<GetRequestedApprovedAmtDto>> GetRequestApprovedCountAsync(DashboardParametersDto dashboardParams)
    {
        var parameters = PrepareParameters(dashboardParams);

        var requestApprovedAmtDto = await ExecuteWithDisabledTracking(async () =>
        {
            var baseQuery = await GetBaseQueryAsync(parameters);
            var applicationQuery = baseQuery.Select(bq => bq.Application).Distinct();

            var applicationTags = await GetFilteredApplicationTags(applicationQuery, parameters);
            var filteredApplications = applicationQuery.Where(app => applicationTags.Contains(app.Id));

            var requestedAmount = filteredApplications.Sum(app => app.RequestedAmount);
            var approvedAmount = filteredApplications.Sum(app => app.ApprovedAmount);

            var queryResult = new List<GetRequestedApprovedAmtDto>
            {
                new GetRequestedApprovedAmtDto { Description = "Requested Amount", Amount = requestedAmount },
                new GetRequestedApprovedAmtDto { Description = "Approved Amount", Amount = approvedAmount }
            };

            return queryResult;
        }); 

        return requestApprovedAmtDto;
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

    private async Task<IQueryable<QueryResult>> GetBaseQueryAsync(DashboardParameters parameters)
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
                    select new QueryResult
                    {
                        Application = application,
                        ApplicationStatus = appStatus,
                        AppAssignee = subAssignee
                    };

        return query;
    }

    private async Task<TResult> ExecuteWithDisabledTracking<TResult>(Func<Task<TResult>> logic)
    {
        using (_intakeRepository.DisableTracking())
        using (_applicationFormRepository.DisableTracking())
        using (_applicationRepository.DisableTracking())
        using (_applicationTagsRepository.DisableTracking())
        using (_applicationStatusRepository.DisableTracking())
        using (_applicationAssignmentRepository.DisableTracking())
        {
            return await logic();
        }
    }

    internal sealed class QueryResult
    {
        public Application Application { get; set; } = new();
        public ApplicationStatus ApplicationStatus { get; set; } = new();
        public ApplicationAssignment AppAssignee { get; set; } = new();
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
}
