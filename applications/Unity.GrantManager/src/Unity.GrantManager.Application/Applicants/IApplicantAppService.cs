using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Unity.Payments.Events;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Applicants;

public interface IApplicantAppService : IApplicationService
{
    Task<Applicant> CreateOrRetrieveApplicantAsync(IntakeMapping intakeMap);
    Task<ApplicantAgent> CreateApplicantAgentAsync(ApplicantAgentDto applicantAgentDto);
    Task<Applicant> RelateSupplierToApplicant(ApplicantSupplierEto applicantSupplierEto);
    Task RelateDefaultSupplierAsync(ApplicantAgentDto applicantAgentDto);
    Task<Applicant> UpdateApplicantOrgMatchAsync(Applicant applicant);
    Task<int> GetNextUnityApplicantIdAsync();
    Task<List<Applicant>> GetApplicantsBySiteIdAsync(Guid siteId);
}
