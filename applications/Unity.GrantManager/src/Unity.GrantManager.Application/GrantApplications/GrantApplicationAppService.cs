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
using Volo.Abp.Validation;

namespace Unity.GrantManager.GrantApplications;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(GrantApplicationAppService), typeof(IGrantApplicationAppService))]
public class GrantApplicationAppService :
    CrudAppService<
    Application,
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
    private readonly IApplicationAssignmentRepository _applicationAssignmentRepository;
    private readonly IApplicantRepository _applicantRepository;
    private readonly ICommentsManager _commentsManager;
    private readonly IApplicationFormRepository _applicationFormRepository;
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IApplicantAgentRepository _applicantAgentRepository;
    private readonly IApplicationTagsRepository _applicationTagsRepository;

#pragma warning disable IDE0290 // Use primary constructor
    public GrantApplicationAppService(IRepository<Application, Guid> repository,
#pragma warning restore IDE0290 // Use primary constructor
        IApplicationManager applicationManager,
        IApplicationRepository applicationRepository,
        IApplicationStatusRepository applicationStatusRepository,
        IApplicationAssignmentRepository applicationAssignmentRepository,
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IApplicantRepository applicantRepository,
        ICommentsManager commentsManager,
        IApplicationFormRepository applicationFormRepository,
        IAssessmentRepository assessmentRepository,
        IPersonRepository personRepository,
        IApplicantAgentRepository applicantAgentRepository,
        IApplicationTagsRepository applicationTagsRepository
        )
         : base(repository)
    {
        _applicationRepository = applicationRepository;
        _applicationManager = applicationManager;
        _applicationStatusRepository = applicationStatusRepository;
        _applicationAssignmentRepository = applicationAssignmentRepository;
        _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
        _applicantRepository = applicantRepository;
        _commentsManager = commentsManager;
        _applicationFormRepository = applicationFormRepository;
        _assessmentRepository = assessmentRepository;
        _personRepository = personRepository;
        _applicantAgentRepository = applicantAgentRepository;
        _applicationTagsRepository = applicationTagsRepository;
    }

    public override async Task<PagedResultDto<GrantApplicationDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        PagedAndSortedResultRequestDto.DefaultMaxResultCount = 1000;

        var query = from application in await _applicationRepository.GetQueryableAsync()
                    join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                    join applicant in await _applicantRepository.GetQueryableAsync() on application.ApplicantId equals applicant.Id
                    join appForm in await _applicationFormRepository.GetQueryableAsync() on application.ApplicationFormId equals appForm.Id
                    join assessment in await _assessmentRepository.GetQueryableAsync() on application.Id equals assessment.ApplicationId into assessments
                    join applicationTag in await _applicationTagsRepository.GetQueryableAsync() on application.Id equals applicationTag.ApplicationId into tags
                    from tag in tags.DefaultIfEmpty()

                    join userAssignment in await _applicationAssignmentRepository.GetQueryableAsync() on application.Id equals userAssignment.ApplicationId into userAssignments
                    from applicationUserAssignment in userAssignments.DefaultIfEmpty()

                    join person in await _personRepository.GetQueryableAsync() on applicationUserAssignment.AssigneeId equals person.Id into persons
                    from applicationPerson in persons.DefaultIfEmpty()

                    join owner in await _personRepository.GetQueryableAsync() on application.OwnerId equals owner.Id into owners
                    from applicationOwner in owners.DefaultIfEmpty()

                    select new
                    {
                        application,
                        appStatus,
                        applicant,
                        appForm,
                        AssessmentCount = assessments.Count(),
                        AssessmentReviewCount = assessments.Count(a => a.Status == AssessmentState.IN_REVIEW),
                        tag,
                        applicationUserAssignment,
                        applicationPerson,
                        applicationOwner
                    };

        var result = query
                .OrderBy(NormalizeSorting(input.Sorting ?? string.Empty))
                .OrderBy(s => s.application.Id)
                .GroupBy(s => s.application.Id)
                .AsEnumerable()
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToList();

        var appDtos = new List<GrantApplicationDto>();
        foreach (var grouping in result)
        {
            var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(grouping.First().application);
            appDto.Status = grouping.First().appStatus.InternalStatus;

            appDto.Applicant = ObjectMapper.Map<Applicant, GrantApplicationApplicantDto>(grouping.First().applicant);
            appDto.Category = grouping.First().appForm.Category ?? string.Empty;
            appDto.AssessmentCount = grouping.First().AssessmentCount;
            appDto.AssessmentReviewCount = grouping.First().AssessmentReviewCount;
            appDto.ApplicationTag = grouping.First().tag?.Text ?? string.Empty;
            appDto.Owner = BuildApplicationOwner(grouping.First().applicationOwner);
            appDto.OrganizationName = grouping.First().applicant?.OrgName ?? string.Empty;
            appDto.OrganizationType = grouping.First().applicant?.OrganizationType ?? string.Empty;
            appDto.Assignees = BuildApplicationAssignees(grouping.Select(s => s.applicationUserAssignment).Where(e => e != null), grouping.Select(s => s.applicationPerson).Where(e => e != null)).ToList();
            appDto.SubStatusDisplayValue = MapSubstatusDisplayValue(appDto.SubStatus);
            appDtos.Add(appDto);
        }

        var totalCount = await _applicationRepository.GetCountAsync();

        return new PagedResultDto<GrantApplicationDto>(totalCount, appDtos);
    }


    private static string MapSubstatusDisplayValue(string subStatus)
    {
        if (subStatus == null) { return string.Empty; }
        var hasKey = AssessmentResultsOptionsList.SubStatusActionList.TryGetValue(subStatus, out string? subStatusValue);
        if (hasKey)
            return subStatusValue ?? string.Empty;
        else
            return string.Empty;
    }

    private static IEnumerable<GrantApplicationAssigneeDto> BuildApplicationAssignees(IEnumerable<ApplicationAssignment> applicationAssignments, IEnumerable<Person> persons)
    {
        foreach (var assignment in applicationAssignments)
        {
            yield return new GrantApplicationAssigneeDto()
            {
                ApplicationId = assignment.ApplicationId,
                AssigneeId = assignment.AssigneeId,
                FullName = persons.FirstOrDefault(s => s.Id == assignment.AssigneeId)?.FullName ?? string.Empty,
                Id = assignment.Id,
                Duty = assignment.Duty
            };
        }
    }

    private static GrantApplicationAssigneeDto BuildApplicationOwner(Person applicationOwner)
    {
        if (applicationOwner != null)
        {
            return new GrantApplicationAssigneeDto()
            {
                Id = applicationOwner.Id,
                FullName = applicationOwner.FullName
            };
        }
        return new GrantApplicationAssigneeDto();
    }

    public override async Task<GrantApplicationDto> GetAsync(Guid id)
    {
        var query = from application in await _applicationRepository.GetQueryableAsync()
                    join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                    join applicant in await _applicantRepository.GetQueryableAsync() on application.ApplicantId equals applicant.Id
                    where application.Id == id
                    select new
                    {
                        application,
                        applicant,
                        appStatus
                    };

        var queryResult = await AsyncExecuter.FirstOrDefaultAsync(query);

        if (queryResult != null)
        {
            var dto = queryResult.application;
            var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(dto);
            appDto.StatusCode = queryResult.appStatus.StatusCode;
            appDto.Status = queryResult.appStatus.InternalStatus;
            appDto.Applicant = ObjectMapper.Map<Applicant, GrantApplicationApplicantDto>(queryResult.applicant);
            var contactInfo = await _applicantAgentRepository.FirstOrDefaultAsync(s => s.ApplicantId == dto.ApplicantId && s.ApplicationId == dto.Id);
            if (contactInfo != null)
            {
                appDto.ContactFullName = contactInfo.Name;
                appDto.ContactEmail = contactInfo.Email;
                appDto.ContactTitle = contactInfo.Title;
                appDto.ContactBusinessPhone = contactInfo.Phone;
                appDto.ContactCellPhone = contactInfo.Phone2;
            }

            if (appDto.Applicant != null)
            {
                appDto.Sector = appDto.Applicant.Sector;
                appDto.SubSector = appDto.Applicant.SubSector;
            }

            return appDto;
        }
        else
        {
            return await Task.FromResult<GrantApplicationDto>(new GrantApplicationDto());
        }
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
                        SubmissionDate = TimeZoneInfo.ConvertTimeFromUtc(application.SubmissionDate, TimeZoneInfo.Local).ToShortDateString(),
                        OrganizationName = applicant.OrgName,
                        OrganizationNumber = applicant.OrgNumber,
                        EconomicRegion = application.EconomicRegion,
                        City = application.City,
                        RequestedAmount = application.RequestedAmount,
                        ProjectBudget = application.TotalProjectBudget,
                        Sector = applicant.Sector,
                        Community = application.Community,
                        Status = application.ApplicationStatus.InternalStatus,
                        LikelihoodOfFunding = application.LikelihoodOfFunding != null && application.LikelihoodOfFunding != "" ? AssessmentResultsOptionsList.FundingList[application.LikelihoodOfFunding] : "",
                        AssessmentStartDate = string.Format("{0:yyyy/MM/dd}", application.AssessmentStartDate),
                        FinalDecisionDate = string.Format("{0:yyyy/MM/dd}", application.FinalDecisionDate),
                        TotalScore = application.TotalScore.ToString(),
                        AssessmentResult = application.AssessmentResultStatus != null && application.AssessmentResultStatus != "" ? AssessmentResultsOptionsList.AssessmentResultStatusList[application.AssessmentResultStatus] : "",
                        RecommendedAmount = application.RecommendedAmount,
                        ApprovedAmount = application.ApprovedAmount,
                        Batch = "", // to-do: ask BA for the implementation of Batch field,                        
                        RegionalDistrict = application.RegionalDistrict,
                        OwnerId = application.OwnerId,

                    };

        var queryResult = await AsyncExecuter.FirstOrDefaultAsync(query);
        if (queryResult != null)
        {
            var ownerId = queryResult.OwnerId ?? Guid.Empty;
            queryResult.Owner = await GetOwnerAsync(ownerId);
            queryResult.Assignees = await GetAssigneesAsync(applicationId);

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

        ValidateLegacyDates(application, input);

        if (application.IsInFinalDecisionState())
        {
            application.UpdateAlwaysChangeableFields(input.Notes, input.SubStatus, input.LikelihoodOfFunding, input.DueDate);

            if (await CurrentUserCanUpdateFieldsPostFinalDecisionAsync()) // User allowed to edit specific fields past approval
            {
                application.UpdateFieldsRequiringPostEditPermission(input.ApprovedAmount, input.RequestedAmount, input.TotalScore);
            }
        }
        else
        {
            application.UpdateAlwaysChangeableFields(input.Notes, input.SubStatus, input.LikelihoodOfFunding, input.DueDate);
            
            if (await CurrentUsCanUpdateAssessmentFieldsAsync())
            {
                application.UpdateFieldsRequiringPostEditPermission(input.ApprovedAmount, input.RequestedAmount, input.TotalScore);

                application.UpdateFieldsOnlyForPreFinalDecision(input.ProjectSummary,
                    input.DueDiligenceStatus,
                    input.TotalProjectBudget,
                    input.RecommendedAmount,
                    input.DeclineRational,
                    input.FinalDecisionDate);

                application.UpdateAssessmentResultStatus(input.AssessmentResultStatus);
            }            
        }

        await _applicationRepository.UpdateAsync(application);
        return ObjectMapper.Map<Application, GrantApplicationDto>(application);
    }

    private async Task<bool> CurrentUsCanUpdateAssessmentFieldsAsync()
    {
        return await AuthorizationService.IsGrantedAsync(GrantApplicationPermissions.AssessmentResults.Edit);
    }

    private async Task<bool> CurrentUserCanUpdateFieldsPostFinalDecisionAsync()
    {
        return await AuthorizationService.IsGrantedAsync(GrantApplicationPermissions.AssessmentResults.EditFinalStateFields);
    }

    private void ValidateLegacyDates(Application application, CreateUpdateGrantApplicationDto input)
    {
        if ((application?.DueDate != input?.DueDate) && (input?.DueDate < DateTime.Now.AddDays(-1)))
        {
            throw new AbpValidationException("Due Date should not be past date.");
        }
        if ((application?.FinalDecisionDate != input?.FinalDecisionDate) && (input?.FinalDecisionDate > DateTime.Now))
        {
            throw new AbpValidationException("Decision Date should not be future date.");
        }
    }

    public async Task<GrantApplicationDto> UpdateProjectInfoAsync(Guid id, CreateUpdateProjectInfoDto input)
    {
        var application = await _applicationRepository.GetAsync(id);
        var percentageTotalProjectBudget = input.TotalProjectBudget == 0 ? 0 : decimal.Multiply(decimal.Divide(input.RequestedAmount ?? 0, input.TotalProjectBudget ?? 0), 100).To<double>();
        if (application != null)
        {
            application.ProjectSummary = input.ProjectSummary;
            application.ProjectName = input.ProjectName ?? String.Empty;
            application.RequestedAmount = input.RequestedAmount ?? 0;
            application.TotalProjectBudget = input.TotalProjectBudget ?? 0;
            application.ProjectStartDate = input.ProjectStartDate;
            application.ProjectEndDate = input.ProjectEndDate;
            application.PercentageTotalProjectBudget = Math.Round(percentageTotalProjectBudget, 2);
            application.ProjectFundingTotal = input.ProjectFundingTotal;
            application.Community = input.Community;
            application.CommunityPopulation = input.CommunityPopulation;
            application.Acquisition = input.Acquisition;
            application.Forestry = input.Forestry;
            application.ForestryFocus = input.ForestryFocus;
            application.EconomicRegion = input.EconomicRegion;
            application.ElectoralDistrict = input.ElectoralDistrict;
            application.RegionalDistrict = input.RegionalDistrict;

            await _applicationRepository.UpdateAsync(application, autoSave: true);

            var applicant = await _applicantRepository.FirstOrDefaultAsync(a => a.Id == application.ApplicantId) ?? throw new EntityNotFoundException();
            // This applicant should never be null!

            applicant.Sector = input.Sector ?? "";
            applicant.SubSector = input.SubSector ?? "";
            _ = await _applicantRepository.UpdateAsync(applicant);

            if (!string.IsNullOrEmpty(input.ContactFullName) || !string.IsNullOrEmpty(input.ContactTitle) || !string.IsNullOrEmpty(input.ContactEmail)
                || !string.IsNullOrEmpty(input.ContactBusinessPhone) || !string.IsNullOrEmpty(input.ContactCellPhone))
            {
                var applicantAgent = await _applicantAgentRepository.FirstOrDefaultAsync(agent => agent.ApplicantId == application.ApplicantId && agent.ApplicationId == application.Id);
                if (applicantAgent == null)
                {
                    applicantAgent = await _applicantAgentRepository.InsertAsync(new ApplicantAgent
                    {
                        ApplicantId = application.ApplicantId,
                        ApplicationId = application.Id,
                        Name = input.ContactFullName ?? "",
                        Phone = input.ContactBusinessPhone ?? "",
                        Phone2 = input.ContactCellPhone ?? "",
                        Email = input.ContactEmail ?? "",
                        Title = input.ContactTitle ?? ""
                    });
                }
                else
                {
                    applicantAgent.Name = input.ContactFullName ?? "";
                    applicantAgent.Phone = input.ContactBusinessPhone ?? "";
                    applicantAgent.Phone2 = input.ContactCellPhone ?? "";
                    applicantAgent.Email = input.ContactEmail ?? "";
                    applicantAgent.Title = input.ContactTitle ?? "";

                    applicantAgent = await _applicantAgentRepository.UpdateAsync(applicantAgent);
                }

                var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(application);

                appDto.ContactFullName = applicantAgent.Name;
                appDto.ContactEmail = applicantAgent.Email;
                appDto.ContactTitle = applicantAgent.Title;
                appDto.ContactBusinessPhone = applicantAgent.Phone;
                appDto.ContactCellPhone = applicantAgent.Phone2;

                return appDto;

            }
            else
            {
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
        var query = from userAssignment in await _applicationAssignmentRepository.GetQueryableAsync()
                    join user in await _personRepository.GetQueryableAsync() on userAssignment.AssigneeId equals user.Id
                    where userAssignment.ApplicationId == applicationId
                    select new GrantApplicationAssigneeDto
                    {
                        Id = userAssignment.Id,
                        AssigneeId = userAssignment.AssigneeId,
                        FullName = user.FullName,
                        Duty = userAssignment.Duty,
                    };

        return query.ToList();
    }

    public async Task<GrantApplicationAssigneeDto> GetOwnerAsync(Guid ownerId)
    {
        try
        {
            var owner = await _personRepository.GetAsync(ownerId, false);


            return new GrantApplicationAssigneeDto
            {
                Id = owner.Id,
                FullName = owner.FullName
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            return new GrantApplicationAssigneeDto();
        }

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

    public async Task InsertAssigneeAsync(Guid applicationId, Guid assigneeId, string? duty)
    {

        try
        {
            var assignees = await GetAssigneesAsync(applicationId);
            if (assignees == null || assignees.FindIndex(a => a.AssigneeId == assigneeId) == -1)
            {
                await _applicationManager.AssignUserAsync(applicationId, assigneeId, duty);
            }
            else
            {
                await _applicationManager.UpdateAssigneeAsync(applicationId, assigneeId, duty);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }

    }

    public async Task DeleteAssigneeAsync(Guid applicationId, Guid assigneeId)
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
    public async Task<IList<GrantApplicationDto>> GetApplicationListAsync(List<Guid> applicationIds)
    {

        var applications = await _applicationRepository.GetListAsync(e => applicationIds.Contains(e.Id));

        return ObjectMapper.Map<List<Application>, List<GrantApplicationDto>>(applications.OrderBy(t => t.Id).ToList());

    }
    public async Task InsertOwnerAsync(Guid applicationId, Guid? assigneeId)
    {

        try
        {
            var application = await _applicationRepository.GetAsync(applicationId, false);
            if (application != null)
            {
                application.OwnerId = assigneeId;
                await _applicationRepository.UpdateAsync(application);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }

    }

    public async Task DeleteOwnerAsync(Guid applicationId)
    {

        try
        {
            var application = await _applicationRepository.GetAsync(applicationId, false);
            if (application != null)
            {
                application.OwnerId = null;
                await _applicationRepository.UpdateAsync(application);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
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
                    var assignees = new List<(Guid? assigneeId, string? fullName)>();

                    foreach (JToken assigneeToken in item.Value.Children())
                    {
                        string? assigneeId = assigneeToken.Value<string?>("assigneeId") ?? null;
                        string? fullName = assigneeToken.Value<string?>("fullName") ?? null;
                        assignees.Add(new(assigneeId != null ? Guid.Parse(assigneeId) : null, fullName));
                    }

                    await _applicationManager.SetAssigneesAsync(currentApplicationId, assignees);
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
