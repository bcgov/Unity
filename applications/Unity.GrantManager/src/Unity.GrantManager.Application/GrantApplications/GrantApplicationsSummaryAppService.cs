using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Identity;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.GrantApplications
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(GrantApplicationsSummaryAppService), typeof(IGrantApplicationsSummaryAppService))]
    public class GrantApplicationsSummaryAppService(
        IApplicationRepository applicationRepository,
        IApplicantRepository applicantRepository,
        IPersonRepository personRepository,
        IApplicationFormRepository applicationFormRepository,
        IApplicationAssignmentRepository applicationAssignmentRepository)
        : GrantManagerAppService, IGrantApplicationsSummaryAppService
    {
        public async Task<GetSummaryDto> GetSummaryAsync(Guid applicationId)
        {
            var query = from application in await applicationRepository.GetQueryableAsync()
                        join applicationForm in await applicationFormRepository.GetQueryableAsync() on application.ApplicationFormId equals applicationForm.Id
                        join applicant in await applicantRepository.GetQueryableAsync() on application.ApplicantId equals applicant.Id
                        where application.Id == applicationId
                        select new GetSummaryDto
                        {
                            Category = applicationForm == null ? string.Empty : applicationForm.Category,
                            SubmissionDate = application.SubmissionDate,
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
                            UnityApplicationId = application.UnityApplicationId
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
                return await Task.FromResult(new GetSummaryDto());
            }
        }

        private async Task<List<GrantApplicationAssigneeDto>> GetAssigneesAsync(Guid applicationId)
        {
            var query = from userAssignment in await applicationAssignmentRepository.GetQueryableAsync()
                        join user in await personRepository.GetQueryableAsync() on userAssignment.AssigneeId equals user.Id
                        where userAssignment.ApplicationId == applicationId
                        select new GrantApplicationAssigneeDto
                        {
                            Id = userAssignment.Id,
                            AssigneeId = userAssignment.AssigneeId,
                            FullName = user.FullName,
                            Duty = userAssignment.Duty,
                            ApplicationId = applicationId
                        };

            return await query.ToListAsync();
        }

        private async Task<GrantApplicationAssigneeDto> GetOwnerAsync(Guid ownerId)
        {
            var owner = await personRepository.FindAsync(ownerId);

            if (owner != null)
            {
                return new GrantApplicationAssigneeDto
                {
                    Id = owner.Id,
                    FullName = owner.FullName
                };
            }
            else
                return new GrantApplicationAssigneeDto();
        }
    }
}
