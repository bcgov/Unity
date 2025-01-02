using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Events;
using Unity.Payments.Suppliers;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Applicants;

public interface IApplicantsAppService : IApplicationService
{
    Task<Applicant> CreateOrRetrieveApplicantAsync(IntakeMapping intakeMap);
    Task<ApplicantAgent> CreateOrUpdateApplicantAgentAsync(ApplicantAgentDto applicantAgentDto);
    Task<Applicant> RelateSupplierToApplicant(ApplicantSupplierEto applicantSupplierEto);
    Task<SupplierDto?> GetSupplierByApplicantIdAsync(Guid applicantId);
    Task<List<Site>> GetSitesBySupplierIdAsync(Guid supplierId);
}
