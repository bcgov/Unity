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
using Unity.GrantManager.Comments;
using Unity.GrantManager.Exceptions;
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

    public GrantApplicationAppService(
        IRepository<GrantApplication, Guid> repository,
        IApplicationManager applicationManager,
        IApplicationRepository applicationRepository,
        IApplicationStatusRepository applicationStatusRepository,
        IApplicationUserAssignmentRepository userAssignmentRepository,
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IApplicantRepository applicantRepository,
        ICommentsManager commentsManager,
        IApplicationFormRepository applicationFormRepository
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
    }

    public override async Task<PagedResultDto<GrantApplicationDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var queryable = await _applicationRepository.GetQueryableAsync();
        PagedAndSortedResultRequestDto.DefaultMaxResultCount = 1000;

        var query = from application in queryable
                    join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                    join applicant in await _applicantRepository.GetQueryableAsync() on application.ApplicantId equals applicant.Id
                    join appForm in await _applicationFormRepository.GetQueryableAsync() on application.ApplicationFormId equals appForm.Id
                    select new { application, appStatus, applicant, appForm };


        query = query
            .OrderBy(NormalizeSorting(input.Sorting))
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
            return appDto;
        }).ToList();

        var applicationDtos = await Task.WhenAll(applicationDtoTasks);

        var totalCount = await _applicationRepository.GetCountAsync();

        return new PagedResultDto<GrantApplicationDto>(
            totalCount,
            applicationDtos
        );
    }

    public async Task<List<GrantApplicationAssigneeDto>> GetAssigneesAsync(Guid applicationId)
    {
        IQueryable<ApplicationUserAssignment> queryableAssignment = (await _userAssignmentRepository.GetQueryableAsync());
        var assignments = queryableAssignment.Where(a => a.ApplicationId.Equals(applicationId)).ToList();
        return ObjectMapper.Map<List<ApplicationUserAssignment>, List<GrantApplicationAssigneeDto>>(assignments);
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
                var dbResult = queryableFormSubmissions.FirstOrDefault(a => a.ApplicationFormId.Equals(application.ApplicationFormId));
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
                throw;
            }
        }
    }

    public async Task InsertAssigneeAsync(Guid[] applicationIds, string AssigneeKeycloakId, string AssigneeDisplayName)
    {
        foreach (Guid applicationId in applicationIds)
        {
            try
            {
                var application = await _applicationRepository.GetAsync(applicationId, true);
                var assignees = await GetAssigneesAsync(applicationId);
                if (application != null && (assignees == null || assignees.FindIndex(a => a.OidcSub == AssigneeKeycloakId) == -1))
                {
                    await _userAssignmentRepository.InsertAsync(
                        new ApplicationUserAssignment
                        {
                            OidcSub = AssigneeKeycloakId,
                            ApplicationId = application.Id,
                            AssigneeDisplayName = AssigneeDisplayName,
                            AssignmentTime = DateTime.Now
                        }
                    );

                    // BUSINES RULE: If an application is in the SUBMITTED state and has
                    // a user assigned, move to the ASSIGNED state.
                    if (application.ApplicationStatus.StatusCode == GrantApplicationState.SUBMITTED)
                    {
                        await _applicationManager.TriggerAction(applicationId, GrantApplicationAction.Internal_Assign);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        
    }

    public async Task DeleteAssigneeAsync(Guid[] applicationIds, string AssigneeKeycloakId)
    {
        foreach (Guid applicationId in applicationIds)
        {
            var application = await _applicationRepository.GetAsync(applicationId, false);
            IQueryable<ApplicationUserAssignment> queryableAssignment = _userAssignmentRepository.GetQueryableAsync().Result;
            var assignments = queryableAssignment.Where(a => a.ApplicationId.Equals(applicationId)).Where(b => b.OidcSub.Equals(AssigneeKeycloakId)).ToList();
            // Only remove the assignee if they were already assigned
            if (application != null && assignments != null)
            {
                var assignment = assignments.FirstOrDefault();
                if (null != assignment)
                {
                    await _userAssignmentRepository.DeleteAsync(assignment);
                }
            }

            // BUSINESS RULE: IF an application has all of its assignees removed,
            // set the application status back to SUBMITTED
            if (!(await GetAssigneesAsync(applicationId)).Any())
            {
                await _applicationManager.TriggerAction(applicationId, GrantApplicationAction.Internal_Unasign);
            }
        }
    }

    [HttpPut]
    public async Task UpdateAssigneesAsync(dynamic modifiedAssignees)
    {
        var dynamicObject = JsonConvert.DeserializeObject<dynamic>(modifiedAssignees);
        if (dynamicObject is IEnumerable)
        {
            string previousApplication = "";
            foreach (JProperty item in dynamicObject)
            {
                string currentApplicationId = item.Name;
                Guid currentGuid = Guid.Parse(currentApplicationId);
                IQueryable<ApplicationUserAssignment> queryableAssignment = _userAssignmentRepository.GetQueryableAsync().Result;
                List<ApplicationUserAssignment> userAssignments = queryableAssignment.Where(a => a.ApplicationId.Equals(currentGuid)).ToList();

                if (currentApplicationId != previousApplication)
                {
                    // Changed applications ids
                    foreach (var userAssignment in userAssignments)
                    {
                        // TODO: ENSURE STATUS IS ENFORCED IF ALL ASSIGNEES ARE REMOVED
                        await _userAssignmentRepository.DeleteAsync(userAssignment);
                    }
                    // Would like to use BatchDeleteAsync
                    await UnitOfWorkManager.Current.SaveChangesAsync();
                }

                foreach (JToken assigneeToken in item.Value.Children())
                {
                    Debug.WriteLine(assigneeToken);
                    string assigneeDisplayName = assigneeToken.Value<string?>("assigneeDisplayName") ?? "";
                    string oidcSub = assigneeToken.Value<string?>("oidcSub") ?? "";
                    Guid[] applicationIds = new Guid[1];
                    applicationIds.SetValue(currentGuid, 0);
                    await InsertAssigneeAsync(applicationIds, oidcSub, assigneeDisplayName);
                }

                // TODO: STATE CHANGE FROM INLINE ASIGNEE EDIT
                //var currentAssignees = await GetAssigneesAsync(currentGuid);
                //var currentApplication = await _applicationRepository.GetAsync(currentGuid, true);
                //if (!currentAssignees.Any())
                //{   
                //    // BUSINESS RULE: IF an application has all of its assignees removed,
                //    // set the application status back to SUBMITTED
                //    await _applicationManager.TriggerAction(currentGuid, GrantApplicationAction.Internal_Unasign);
                //}
                //else if (currentApplication.ApplicationStatus.StatusCode == GrantApplicationState.SUBMITTED)
                //{
                //    // BUSINES RULE: If an application is in the SUBMITTED state and has
                //    // a user assigned, move to the ASSIGNED state.
                //    await _applicationManager.TriggerAction(currentGuid, GrantApplicationAction.Internal_Assign);
                //}

                previousApplication = currentApplicationId;
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
