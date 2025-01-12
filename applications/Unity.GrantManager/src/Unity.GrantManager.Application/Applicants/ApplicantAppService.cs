using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;
using Unity.GrantManager.Intakes;
using System;
using Unity.GrantManager.Intakes.Mapping;
using Unity.GrantManager.GrantApplications;
using Unity.Payments.Events;
using Volo.Abp;

namespace Unity.GrantManager.Applicants;


[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicantAppService), typeof(IApplicantAppService))]
public class ApplicantAppService(IApplicantRepository applicantRepository,
                                 IApplicantAddressRepository addressRepository,
                                 IApplicantAgentRepository applicantAgentRepository) : GrantManagerAppService, IApplicantAppService
{

    [RemoteService(false)]
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

    [RemoteService(false)]
    public async Task<Applicant> RelateSupplierToApplicant(ApplicantSupplierEto applicantSupplierEto)
    {
        ArgumentNullException.ThrowIfNull(applicantSupplierEto.ApplicantId);
        Applicant? applicant = await applicantRepository.GetAsync(applicantSupplierEto.ApplicantId);
        ArgumentNullException.ThrowIfNull(applicant);
        applicant.SupplierId = applicantSupplierEto.SupplierId;
        await applicantRepository.UpdateAsync(applicant);
        return applicant;
    }

    [RemoteService(false)]
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
            RedStop = false,
            Indigenous = intakeMap.Indigenous ?? "N",
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
}