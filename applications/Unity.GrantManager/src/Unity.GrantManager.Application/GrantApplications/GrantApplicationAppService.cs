using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using Volo.Abp.DependencyInjection;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Comments;
using Volo.Abp.Domain.Entities;
using Unity.GrantManager.Exceptions;
using Volo.Abp.Users;

namespace Unity.GrantManager.GrantApplications
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(GrantApplicationAppService), typeof(IGrantApplicationAppService))]
    public class GrantApplicationAppService :
        CrudAppService<
        GrantApplication,
        GrantApplicationDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateGrantApplicationDto>
        , IGrantApplicationAppService
    {

        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationStatusRepository _applicationStatusRepository;
        private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
        private readonly IApplicationUserAssignmentRepository _userAssignmentRepository;
        private readonly ICommentsManager _commentsManager;                

        public GrantApplicationAppService(
            IRepository<GrantApplication, Guid> repository,
            IApplicationRepository applicationRepository,
            IApplicationStatusRepository applicationStatusRepository,
            IApplicationUserAssignmentRepository userAssignmentRepository,
            IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
            ICommentsManager commentsManager
            )
             : base(repository)
        {
            _applicationRepository = applicationRepository;
            _applicationStatusRepository = applicationStatusRepository;
            _userAssignmentRepository = userAssignmentRepository;
            _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
            _commentsManager = commentsManager;
        }

        public override async Task<PagedResultDto<GrantApplicationDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            //Get the IQueryable<Book> from the repository
            var queryable = await _applicationRepository.GetQueryableAsync();

            //Prepare a query to join books and authors
            var query = from application in queryable
                        join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                        select new { application, appStatus };

            try {
                query = query
                    .OrderBy(NormalizeSorting(input.Sorting))
                    .Skip(input.SkipCount)
                    .Take(input.MaxResultCount);
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            //Execute the query and get a list
            var queryResult = await AsyncExecuter.ToListAsync(query);


            //Convert the query result to a list of ApplicationDto objects
            var applicationDtos = queryResult.Select(x =>
            {
                var appDto = ObjectMapper.Map<Application, GrantApplicationDto>(x.application);
                appDto.Status = x.appStatus.InternalStatus;
                appDto.Assignees = getAssignees(x.application.Id);
                return appDto;
            }).ToList();

            //Get the total count with another query
            var totalCount = await _applicationRepository.GetCountAsync();

            return new PagedResultDto<GrantApplicationDto>(
                totalCount,
                applicationDtos
            );
        }

        public List<GrantApplicationAssigneeDto> getAssignees(Guid applicationId)
        {
            IQueryable<ApplicationUserAssignment> queryableAssignment = _userAssignmentRepository.GetQueryableAsync().Result;
            var assignments = queryableAssignment.Where(a => a.ApplicationId.Equals(applicationId)).ToList();

            var assignees = ObjectMapper.Map<List<ApplicationUserAssignment>, List<GrantApplicationAssigneeDto>>(assignments);
            return assignees;
        }

        public async Task<ApplicationFormSubmission> GetFormSubmissionByApplicationId(Guid applicationId)
        {
            ApplicationFormSubmission applicationFormSubmission = new ApplicationFormSubmission();
            var application = await _applicationRepository.GetAsync(applicationId);

            if (application != null)
            {
                IQueryable<ApplicationFormSubmission> queryableFormSubmissions = _applicationFormSubmissionRepository.GetQueryableAsync().Result;

                if (queryableFormSubmissions != null)
                {
                    applicationFormSubmission = queryableFormSubmissions.Where(a => a.ApplicationFormId.Equals(application.ApplicationFormId)).FirstOrDefault();
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
                    var application = await _applicationRepository.GetAsync(applicationId);
                    if (application != null)
                    {
                        application.ApplicationStatusId = statusId;
                        await _applicationRepository.UpdateAsync(application);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    throw new Exception(ex.ToString());
                }

            }
        }

        public async Task AddAssignee(Guid[] applicationIds, string AssigneeKeycloakId, string AssigneeDisplayName)
        {
            foreach (Guid applicationId in applicationIds)
            {
                try
                {
                    var application = await _applicationRepository.GetAsync(applicationId);
                    if (application != null)
                    {
                        await _userAssignmentRepository.InsertAsync(
                            new ApplicationUserAssignment
                            {
                                OidcSub = AssigneeKeycloakId,
                                //ApplicationFormId = appForm1.Id,
                                ApplicationId = application.Id,
                                AssigneeDisplayName = AssigneeDisplayName,
                                AssignmentTime = DateTime.Now
                            }
                            );
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
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
    }
}
