using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;
using Unity.GrantManager.Intakes;
using System;
using Unity.GrantManager.Intakes.Mapping;
using Unity.GrantManager.GrantApplications;
using Unity.Payments.Suppliers;
using Unity.Modules.Shared.Correlation;
using Unity.Payments.Events;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Unity.Payments.Domain.Suppliers;

namespace Unity.GrantManager.Applicants;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicantAppService), typeof(IApplicantsAppService))]
public class ApplicantAppService(ISiteRepository siteRepository,
                                 IApplicantRepository applicantRepository,
                                 ISupplierAppService supplierAppService,
                                 IApplicantAddressRepository addressRepository,
                                 IApplicantAgentRepository applicantAgentRepository) : GrantManagerAppService, IApplicantsAppService
{

    public async Task<Applicant> CreateOrRetrieveApplicantAsync(IntakeMapping intakeMap)
    {
        ArgumentNullException.ThrowIfNull(intakeMap);

        Applicant? applicant = await GetExistingApplicantAsync(intakeMap.UnityApplicantId);
        if (applicant != null)
        {
            return applicant;
        }

        applicant = await CreateNewApplicantAsync(intakeMap);
        await CreateApplicantAddressesAsync(intakeMap, applicant);
        return applicant;
    }

    public async Task<Applicant> RelateSupplierToApplicant(ApplicantSupplierEto applicantSupplierEto)
    {
        ArgumentNullException.ThrowIfNull(applicantSupplierEto.ApplicantId);
        Applicant? applicant = await applicantRepository.GetAsync(applicantSupplierEto.ApplicantId);
        ArgumentNullException.ThrowIfNull(applicant);
        applicant.SupplierId = applicantSupplierEto.SupplierId;
        await applicantRepository.UpdateAsync(applicant);
        return applicant;
    }

    public async Task<List<Site>> GetSitesBySupplierIdAsync(Guid supplierId)
    {
        return await siteRepository.GetBySupplierAsync(supplierId);
    }

    public async Task<SupplierDto?> GetSupplierByApplicantIdAsync(Guid applicantId)
    {

        Applicant applicant = await applicantRepository.GetAsync(applicantId);

        //If SupplierId is available, use it to get the supplier
        if (applicant.SupplierId.HasValue)
        {
            Guid supplierId = applicant.SupplierId.Value;
            return await supplierAppService.GetAsync(supplierId);
        }

        // If no SupplierId, fetch the supplier using the correlation
        return await supplierAppService.GetByCorrelationAsync(new GetSupplierByCorrelationDto()
        {
            CorrelationId = applicantId,
            CorrelationProvider = CorrelationConsts.Applicant,
            IncludeDetails = true
        });
    }

    private async Task<Applicant?> GetExistingApplicantAsync(string? unityApplicantId)
    {
        if (unityApplicantId.IsNullOrEmpty()) return null;
        return await applicantRepository.GetByUnityApplicantIdAsync(unityApplicantId);
    }

    private async Task<Applicant> CreateNewApplicantAsync(IntakeMapping intakeMap)
    {
        ArgumentNullException.ThrowIfNull(intakeMap);

        var applicant = new Applicant
        {
            ApplicantName = MappingUtil.ResolveAndTruncateField(600, string.Empty, intakeMap.ApplicantName),
            NonRegisteredBusinessName = intakeMap.NonRegisteredBusinessName,
            OrgName = intakeMap.OrgName,
            OrgNumber = intakeMap.OrgNumber,
            OrganizationType = intakeMap.OrganizationType,
            Sector = intakeMap.Sector,
            SubSector = intakeMap.SubSector,
            SectorSubSectorIndustryDesc = intakeMap.SectorSubSectorIndustryDesc,
            ApproxNumberOfEmployees = intakeMap.ApproxNumberOfEmployees,
            IndigenousOrgInd = intakeMap.IndigenousOrgInd ?? "N",
            OrgStatus = intakeMap.OrgStatus,
            RedStop = false
        };

        return await applicantRepository.InsertAsync(applicant);
    }

    private async Task CreateApplicantAddressesAsync(IntakeMapping intakeMap, Applicant applicant)
    {
        ArgumentNullException.ThrowIfNull(intakeMap);

        if (!intakeMap.PhysicalStreet.IsNullOrEmpty()
            || !intakeMap.PhysicalStreet2.IsNullOrEmpty())
        {
            await addressRepository.InsertAsync(new ApplicantAddress
            {
                ApplicantId = applicant.Id,
                City = intakeMap.PhysicalCity,
                Country = intakeMap.PhysicalCountry,
                Province = intakeMap.PhysicalProvince,
                Postal = intakeMap.PhysicalPostal,
                Street = intakeMap.PhysicalStreet,
                Street2 = intakeMap.PhysicalStreet2,
                Unit = intakeMap.PhysicalUnit,
                AddressType = AddressType.PhysicalAddress
            });
        }

        if (!intakeMap.MailingStreet.IsNullOrEmpty()
            || !intakeMap.MailingStreet2.IsNullOrEmpty())
        {
            await addressRepository.InsertAsync(new ApplicantAddress
            {
                ApplicantId = applicant.Id,
                City = intakeMap.MailingCity,
                Country = intakeMap.MailingCountry,
                Province = intakeMap.MailingProvince,
                Postal = intakeMap.MailingPostal,
                Street = intakeMap.MailingStreet,
                Street2 = intakeMap.MailingStreet2,
                Unit = intakeMap.MailingUnit,
                AddressType = AddressType.MailingAddress
            });
        }
    }

    public async Task<ApplicantAgent> CreateOrUpdateApplicantAgentAsync(ApplicantAgentDto applicantAgentDto)
    {
        var applicant = applicantAgentDto.Applicant;
        var application = applicantAgentDto.Application;
        var intakeMap = applicantAgentDto.IntakeMap;
        var applicantAgent = new ApplicantAgent
        {
            ApplicantId = applicant.Id,
            ApplicationId = application.Id,
            Name = intakeMap.ContactName ?? string.Empty,
            Phone = intakeMap.ContactPhone ?? string.Empty,
            Phone2 = intakeMap.ContactPhone2 ?? string.Empty,
            Email = intakeMap.ContactEmail ?? string.Empty,
            Title = intakeMap.ContactTitle ?? string.Empty,
        };

        if (MappingUtil.IsJObject(intakeMap.ApplicantAgent))
        {
            applicantAgent.BceidUserGuid = intakeMap.ApplicantAgent?.bceid_user_guid ?? Guid.Empty;
            applicantAgent.BceidBusinessGuid = intakeMap.ApplicantAgent?.bceid_business_guid ?? Guid.Empty;
            applicantAgent.BceidBusinessName = intakeMap.ApplicantAgent?.bceid_business_name ?? "";
            applicantAgent.BceidUserName = intakeMap.ApplicantAgent?.bceid_username ?? "";
            applicantAgent.IdentityProvider = intakeMap.ApplicantAgent?.identity_provider ?? "";
            applicantAgent.IdentityName = intakeMap.ApplicantAgent?.name ?? "";
            applicantAgent.IdentityEmail = intakeMap.ApplicantAgent?.email ?? "";
        }
        await applicantAgentRepository.InsertAsync(applicantAgent);

        return applicantAgent;
    }

}