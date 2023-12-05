using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Comments;
using Unity.GrantManager.Exceptions;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.GrantApplications;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(GrantApplicationAppService), typeof(IGrantApplicationAppService))]
public class GrantApplicationAppService :
    CrudAppService<
    GrantApplication,
    GrantApplicationDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateGrantApplicationDto>,
    IGrantApplicationAppService
{

    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationManager _applicationManager;
    private readonly IApplicationStatusRepository _applicationStatusRepository;
    private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
    private readonly IApplicationUserAssignmentRepository _userAssignmentRepository;
    private readonly IApplicantRepository _applicantRepository;
    private readonly ICommentsManager _commentsManager;
    private readonly IApplicationFormRepository _applicationFormRepository;
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly ITenantUserRepository _tenantUserRepository;

    public GrantApplicationAppService(
        IRepository<GrantApplication, Guid> repository,
        IApplicationManager applicationManager,
        IApplicationRepository applicationRepository,
        IApplicationStatusRepository applicationStatusRepository,
        IApplicationUserAssignmentRepository userAssignmentRepository,
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IApplicantRepository applicantRepository,
        ICommentsManager commentsManager,
        IApplicationFormRepository applicationFormRepository,
        IAssessmentRepository assessmentRepository,
        ITenantUserRepository tenantUserRepository
        )
         : base(repository)
    {
        _applicationRepository = applicationRepository;
        _applicationManager = applicationManager;
        _applicationStatusRepository = applicationStatusRepository;
        _userAssignmentRepository = userAssignmentRepository;
        _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
        _applicantRepository = applicantRepository;
        _commentsManager = commentsManager;
        _applicationFormRepository = applicationFormRepository;
        _assessmentRepository = assessmentRepository;
        _tenantUserRepository = tenantUserRepository;
    }

    public override async Task<PagedResultDto<GrantApplicationDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var applicationQueryable = await _applicationRepository.GetQueryableAsync();
        PagedAndSortedResultRequestDto.DefaultMaxResultCount = 1000;

        var query = from application in applicationQueryable
                    join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                    join applicant in await _applicantRepository.GetQueryableAsync() on application.ApplicantId equals applicant.Id
                    join appForm in await _applicationFormRepository.GetQueryableAsync() on application.ApplicationFormId equals appForm.Id
                    join assessment in await _assessmentRepository.GetQueryableAsync() on application.Id equals assessment.ApplicationId into assessments
                    select new
                    {
                        application,
                        appStatus,
                        applicant,
                        appForm,
                        AssessmentCount = assessments.Count(),
                        AssessmentReviewCount = assessments.Count(a => a.Status == AssessmentState.IN_REVIEW)
                    };

        query = query
            .OrderBy(NormalizeSorting(input.Sorting ?? string.Empty))
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var queryResult = await AsyncExecuter.ToListAsync(query);

        var applicationDtoTasks = queryResult.Select(async x =>
        {
            var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(x.application);
            appDto.Status = x.appStatus.InternalStatus;
            appDto.Assignees = await GetAssigneesAsync(x.application.Id);
            appDto.Applicant = x.applicant.ApplicantName;
            appDto.Category = x.appForm.Category ?? string.Empty;
            appDto.AssessmentCount = x.AssessmentCount;
            appDto.AssessmentReviewCount = x.AssessmentReviewCount;
            return appDto;
        }).ToList();

        var applicationDtos = await Task.WhenAll(applicationDtoTasks);

        var totalCount = await _applicationRepository.GetCountAsync();

        return new PagedResultDto<GrantApplicationDto>(
            totalCount,
            applicationDtos
        );
    }

    public override async Task<GrantApplicationDto> GetAsync(Guid id)
    {
        var dto = await _applicationRepository.GetAsync(id);
        var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(dto);
        appDto.StatusCode = dto.ApplicationStatus.StatusCode;
        return appDto;
    }

    public async Task<GetSummaryDto> GetSummaryAsync(Guid applicationId)
    {
        var query = from application in await _applicationRepository.GetQueryableAsync()
                    join applicationForm in await _applicationFormRepository.GetQueryableAsync() on application.ApplicationFormId equals applicationForm.Id
                    join applicant in await _applicantRepository.GetQueryableAsync() on application.ApplicantId equals applicant.Id
                    where application.Id == applicationId
                    select new GetSummaryDto
                    {
                        Category = applicationForm == null ? string.Empty : applicationForm.Category,
                        SubmissionDate = application.CreationTime.ToShortDateString(),
                        OrganizationName = applicant.OrgName,
                        OrganizationNumber = applicant.OrgNumber,
                        EconomicRegion = application.EconomicRegion,
                        City = application.City,
                        RequestedAmount = application.RequestedAmount,
                        ProjectBudget = application.TotalProjectBudget,
                        Sector = application.Sector,
                        Community = applicant.Community,
                        Status = application.ApplicationStatus.InternalStatus,
                        LikelihoodOfFunding = application.LikelihoodOfFunding != null && application.LikelihoodOfFunding != "" ? AssessmentResultsOptionsList.FundingList[application.LikelihoodOfFunding] : "",
                        AssessmentStartDate = string.Format("{0:MM/dd/yyyy}", application.AssessmentStartDate),
                        FinalDecisionDate = string.Format("{0:MM/dd/yyyy}", application.FinalDecisionDate),
                        TotalScore = application.TotalScore.ToString(),
                        AssessmentResult = application.AssessmentResultStatus != null && application.AssessmentResultStatus != "" ? AssessmentResultsOptionsList.AssessmentResultStatusList[application.AssessmentResultStatus] : "",
                        RecommendedAmount = application.RecommendedAmount,
                        ApprovedAmount = application.ApprovedAmount,
                        Batch = "" // to-do: ask BA for the implementation of Batch field
                    };

        var queryResult = await AsyncExecuter.FirstOrDefaultAsync(query);
        if (queryResult != null)
        {
            return queryResult;
        }
        else
        {
            return await Task.FromResult<GetSummaryDto>(new GetSummaryDto());
        }

    }

    public override async Task<GrantApplicationDto> UpdateAsync(Guid id, CreateUpdateGrantApplicationDto input)
    {
        var application = await _applicationRepository.GetAsync(id);
        if (application != null)
        {
            bool IsEditGranted = await AuthorizationService.IsGrantedAsync(GrantApplicationPermissions.AssessmentResults.Edit);
            bool IsEditApprovedAmount = await AuthorizationService.IsGrantedAsync(GrantApplicationPermissions.AssessmentResults.EditApprovedAmount);
            bool IsFinalDecisionMade = GrantApplicationStateGroups.FinalDecisionStates.Contains(application.ApplicationStatus.StatusCode);
            if (!IsEditGranted)
            {
                throw new UnauthorizedAccessException(message: "No permission to update");
            }
            else if (IsFinalDecisionMade && !IsEditApprovedAmount)
            {
                throw new UnauthorizedAccessException(message: "Final decision is made, Update not allowed!");
            }
            else
            {
                // These are some business rules that should be pushed further down into domain
                if (IsEditApprovedAmount && IsFinalDecisionMade) // Only users with EditApprovedAmount permission can edit the value after final decision
                {
                    application.ApprovedAmount = input.ApprovedAmount ?? 0;
                }
                else
                {
                    application.ProjectSummary = input.ProjectSummary;
                    application.RequestedAmount = input.RequestedAmount ?? 0;
                    application.TotalProjectBudget = input.TotalProjectBudget ?? 0;
                    application.RecommendedAmount = input.RecommendedAmount ?? 0;
                    application.ApprovedAmount = input.ApprovedAmount ?? 0;
                    application.LikelihoodOfFunding = input.LikelihoodOfFunding;
                    application.DueDilligenceStatus = input.DueDiligenceStatus;
                    application.Recommendation = input.Recommendation;
                    application.DeclineRational = input.DeclineRational;
                    application.TotalScore = input.TotalScore;
                    application.Notes = input.Notes;
                    if (input.AssessmentResultStatus != application.AssessmentResultStatus)
                    {
                        application.AssessmentResultDate = DateTime.UtcNow;
                    }
                    application.AssessmentResultStatus = input.AssessmentResultStatus;
                    application.FinalDecisionDate = input.FinalDecisionDate;
                }

                await _applicationRepository.UpdateAsync(application, autoSave: true);

                return ObjectMapper.Map<Application, GrantApplicationDto>(application);
            }
        }
        else
        {
            throw new EntityNotFoundException();
        }
    }

    public async Task<List<GrantApplicationAssigneeDto>> GetAssigneesAsync(Guid applicationId)
    {
        var query = from userAssignment in await _userAssignmentRepository.GetQueryableAsync()
                    join user in await _tenantUserRepository.GetQueryableAsync() on userAssignment.AssigneeId equals user.Id
                    where userAssignment.ApplicationId == applicationId
                    select new GrantApplicationAssigneeDto
                    {
                        Id = userAssignment.Id,
                        AssigneeId = userAssignment.AssigneeId,
                        FullName = user.FullName
                    };

        return query.ToList();
    }

    public async Task<ApplicationFormSubmission> GetFormSubmissionByApplicationId(Guid applicationId)
    {
        ApplicationFormSubmission applicationFormSubmission = new();
        var application = await _applicationRepository.GetAsync(applicationId, false);
        if (application != null)
        {
            IQueryable<ApplicationFormSubmission> queryableFormSubmissions = _applicationFormSubmissionRepository.GetQueryableAsync().Result;
            if (queryableFormSubmissions != null)
            {
                var dbResult = queryableFormSubmissions
                    .FirstOrDefault(a => a.ApplicationId.Equals(applicationId));

                if (dbResult != null)
                {
                    applicationFormSubmission = dbResult;
                }
            }
        }
        return applicationFormSubmission;
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
        foreach (Guid applicationId in applicationIds)
        {
            try
            {
                var application = await _applicationRepository.GetAsync(applicationId, false);
                if (application != null)
                {
                    application.ApplicationStatusId = statusId;
                    await _applicationRepository.UpdateAsync(application);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }

    public async Task InsertAssigneeAsync(Guid[] applicationIds, Guid assigneeId)
    {
        foreach (Guid applicationId in applicationIds)
        {
            try
            {
                var assignees = await GetAssigneesAsync(applicationId);
                if (assignees == null || assignees.FindIndex(a => a.AssigneeId == assigneeId) == -1)
                {
                    await _applicationManager.AssignUserAsync(applicationId, assigneeId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }

    public async Task DeleteAssigneeAsync(Guid[] applicationIds, Guid assigneeId)
    {
        foreach (Guid applicationId in applicationIds)
        {
            try
            {
                await _applicationManager.RemoveAssigneeAsync(applicationId, assigneeId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }

    [HttpPut]
    public async Task UpdateAssigneesAsync(dynamic modifiedAssignees)
    {
        var dynamicObject = JsonConvert.DeserializeObject<dynamic>(modifiedAssignees);
        if (dynamicObject is IEnumerable)
        {
            Guid previousApplicationId = Guid.Empty;
            foreach (JProperty item in dynamicObject)
            {
                Guid currentApplicationId = Guid.Parse(item.Name);
                if (currentApplicationId != previousApplicationId)
                {
                    var oidcSubs = new List<(Guid? assigneeId, string? fullName)>();

                    foreach (JToken assigneeToken in item.Value.Children())
                    {
                        string? assigneeId = assigneeToken.Value<string?>("assigneeId") ?? null;
                        string? fullName = assigneeToken.Value<string?>("fullName") ?? null;
                        oidcSubs.Add(new(assigneeId != null ? Guid.Parse(assigneeId) : null, fullName));
                    }

                    await _applicationManager.SetAssigneesAsync(currentApplicationId, oidcSubs);
                }

                previousApplicationId = currentApplicationId;
            }
        }
    }

    public async Task<CommentDto> CreateCommentAsync(Guid id, CreateCommentDto dto)
    {
        return ObjectMapper.Map<ApplicationComment, CommentDto>((ApplicationComment)
         await _commentsManager.CreateCommentAsync(id, dto.Comment, CommentType.ApplicationComment));
    }

    public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid id)
    {
        return ObjectMapper.Map<IReadOnlyList<ApplicationComment>, IReadOnlyList<CommentDto>>((IReadOnlyList<ApplicationComment>)
            await _commentsManager.GetCommentsAsync(id, CommentType.ApplicationComment));
    }

    public async Task<CommentDto> UpdateCommentAsync(Guid id, UpdateCommentDto dto)
    {
        try
        {
            return ObjectMapper.Map<ApplicationComment, CommentDto>((ApplicationComment)
                await _commentsManager.UpdateCommentAsync(id, dto.CommentId, dto.Comment, CommentType.ApplicationComment));

        }
        catch (EntityNotFoundException)
        {
            throw new InvalidCommentParametersException();
        }
    }

    public async Task<CommentDto> GetCommentAsync(Guid id, Guid commentId)
    {
        var comment = await _commentsManager.GetCommentAsync(id, commentId, CommentType.ApplicationComment);

        return comment == null
            ? throw new InvalidCommentParametersException()
            : ObjectMapper.Map<ApplicationComment, CommentDto>((ApplicationComment)comment);
    }

    #region APPLICATION WORKFLOW
    public async Task<ApplicationStatusDto> GetApplicationStatusAsync(Guid id)
    {
        var application = await _applicationRepository.GetAsync(id, true);
        return ObjectMapper.Map<ApplicationStatus, ApplicationStatusDto>(await _applicationStatusRepository.GetAsync(application.ApplicationStatusId));
    }

    /// <summary>
    /// Fetches the list of actions and their status context for a given application.
    /// </summary>
    /// <param name="applicationId">The application</param>
    /// <returns>A list of application actions with their state machine permitted and authorization status.</returns>
    public async Task<ListResultDto<ApplicationActionDto>> GetActions(Guid applicationId, bool includeInternal = false)
    {
        var actionList = await _applicationManager.GetActions(applicationId);

        // Note: Remove internal state change actions that are side-effects of domain events
        var externalActionsList = actionList.Where(a => includeInternal || !a.IsInternal).ToList();
        var actionDtos = ObjectMapper.Map<
            List<ApplicationActionResultItem>,
            List<ApplicationActionDto>>(externalActionsList);

        // NOTE: Authorization is applied on the AppService layer and is false by default
        // TODO: Replace placeholder loop with authorization handler mapped to permissions
        // AUTHORIZATION HANDLING
        actionDtos.ForEach(item => { item.IsAuthorized = true; });

        return new ListResultDto<ApplicationActionDto>(actionDtos);
    }

    /// <summary>
    /// Transitions the Application workflow state machine given an action.
    /// </summary>
    /// <param name="applicationId">The application</param>
    /// <param name="triggerAction">The action to be invoked on an Application</param>
    public async Task<GrantApplicationDto> TriggerAction(Guid applicationId, GrantApplicationAction triggerAction)
    {
        var application = await _applicationManager.TriggerAction(applicationId, triggerAction);
        // TODO: AUTHORIZATION HANDLING
        return ObjectMapper.Map<Application, GrantApplicationDto>(application);
    }
    #endregion APPLICATION WORKFLOW
}
