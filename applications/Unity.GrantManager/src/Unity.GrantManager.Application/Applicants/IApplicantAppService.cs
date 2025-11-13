using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Intakes;
using Unity.Modules.Shared;
using Unity.Payments.Events;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Applicants;

public interface IApplicantAppService : IApplicationService
{
    Task<Applicant> CreateOrRetrieveApplicantAsync(IntakeMapping intakeMap, Guid applicationId);
    Task<ApplicantAgent> CreateApplicantAgentAsync(ApplicantAgentDto applicantAgentDto);
    Task<Applicant> RelateSupplierToApplicant(ApplicantSupplierEto applicantSupplierEto);
    Task RelateDefaultSupplierAsync(ApplicantAgentDto applicantAgentDto);
    Task<Applicant> UpdateApplicantOrgMatchAsync(Applicant applicant);
    Task<int> GetNextUnityApplicantIdAsync();
    Task<List<Applicant>> GetApplicantsBySiteIdAsync(Guid siteId);
    Task<JsonDocument> GetApplicantLookUpAutocompleteQueryAsync(string? applicantLookUpQuery);
    Task<PagedResultDto<ApplicantListDto>> GetListAsync(ApplicantListRequestDto input);
    Task<Applicant> PartialUpdateApplicantSummaryAsync(Guid applicantId, PartialUpdateDto<UpdateApplicantSummaryDto> input);
    Task UpdateApplicantContactAddressesAsync(Guid applicantId, UpdateApplicantContactAddressesDto input);
}
