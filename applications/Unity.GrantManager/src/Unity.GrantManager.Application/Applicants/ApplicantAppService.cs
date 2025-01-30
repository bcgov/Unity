using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;
using Unity.GrantManager.Intakes;
using System;
using Unity.GrantManager.Intakes.Mapping;
using Unity.GrantManager.GrantApplications;
using Unity.Payments.Events;
using Volo.Abp;
using System.Collections.Generic;
using Unity.GrantManager.Integration.Orgbook;
using Newtonsoft.Json.Linq;
using System.Linq;
using Unity.Modules.Shared.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Applicants;


[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicantAppService), typeof(IApplicantAppService))]
public class ApplicantAppService(IApplicantRepository applicantRepository,
                                 IApplicantAddressRepository addressRepository,
                                 IOrgBookService orgBookService,
                                 IApplicantAgentRepository applicantAgentRepository) : GrantManagerAppService, IApplicantAppService
{   protected ILogger Logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

    [RemoteService(false)]
    public async Task<Applicant> CreateOrRetrieveApplicantAsync(IntakeMapping intakeMap)
    {
        ArgumentNullException.ThrowIfNull(intakeMap);

        Applicant? applicant = await GetExistingApplicantAsync(intakeMap.UnityApplicantId);
        if (applicant == null)
        {
            applicant = await CreateNewApplicantAsync(intakeMap);
        } else {
            applicant.ApplicantName = MappingUtil.ResolveAndTruncateField(600, string.Empty, intakeMap.ApplicantName) ?? applicant.ApplicantName;
            applicant.NonRegisteredBusinessName = intakeMap.NonRegisteredBusinessName ?? applicant.NonRegisteredBusinessName;
            applicant.OrgName = intakeMap.OrgName ?? applicant.OrgName;
            applicant.OrgNumber = intakeMap.OrgNumber ?? applicant.OrgNumber;
            applicant.OrganizationType = intakeMap.OrganizationType ?? applicant.OrganizationType;
            applicant.Sector = intakeMap.Sector ?? applicant.Sector;
            applicant.SubSector = intakeMap.SubSector ?? applicant.SubSector;
            applicant.SectorSubSectorIndustryDesc = intakeMap.SectorSubSectorIndustryDesc ?? applicant.SectorSubSectorIndustryDesc;
            applicant.ApproxNumberOfEmployees = intakeMap.ApproxNumberOfEmployees ?? applicant.ApproxNumberOfEmployees;
            applicant.IndigenousOrgInd = intakeMap.IndigenousOrgInd ?? applicant.IndigenousOrgInd;
            applicant.OrgStatus = intakeMap.OrgStatus  ?? applicant.OrgStatus;
            applicant.FiscalDay = MappingUtil.ConvertToIntFromString(intakeMap.FiscalDay) ?? applicant.FiscalDay;
            applicant.FiscalMonth = intakeMap.FiscalMonth ?? applicant.FiscalMonth;
        }

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
        var applicantAgent = await applicantAgentRepository.GetByApplicantIdAsync(applicant.Id);
        bool applicantAgentExists = applicantAgent != null;
        if(applicantAgent != null) {
            applicantAgent.Name = intakeMap.ContactName ?? applicantAgent.Name;
            applicantAgent.Phone = intakeMap.ContactPhone ?? applicantAgent.Phone;
            applicantAgent.Phone2 = intakeMap.ContactPhone2 ?? applicantAgent.Phone2;
            applicantAgent.Email = intakeMap.ContactEmail ?? applicantAgent.Email;
            applicantAgent.Title = intakeMap.ContactTitle ?? applicantAgent.Title;
        } else {
            applicantAgent = new ApplicantAgent
            {
                ApplicantId = applicant.Id,
                ApplicationId = application.Id,
                Name = intakeMap.ContactName ?? string.Empty,
                Phone = intakeMap.ContactPhone ?? string.Empty,
                Phone2 = intakeMap.ContactPhone2 ?? string.Empty,
                Email = intakeMap.ContactEmail ?? string.Empty,
                Title = intakeMap.ContactTitle ?? string.Empty,
            };
        }

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

        if (applicantAgentExists)
        {
            await applicantAgentRepository.UpdateAsync(applicantAgent);
        }
        else
        {
            await applicantAgentRepository.InsertAsync(applicantAgent);
        }

        return applicantAgent;
    }

    [RemoteService(true)]
    public async Task MatchApplicantOrgNamesAsync()
    {
        List<Applicant> applicants = await applicantRepository.GetUnmatchedApplicants();
        foreach (Applicant applicant in applicants)
        {
            // Create match lookups for applicants with org numbers but no org names
            if (applicant.OrgNumber.IsNullOrEmpty() || !applicant.OrgName.IsNullOrEmpty()) continue;
            await UpdateApplicantOrgMatchAsync(applicant);
        }
    }

    private async Task UpdateApplicantOrgMatchAsync(Applicant applicant)
    {
        try
        {
            if (string.IsNullOrEmpty(applicant.OrgNumber)) return;
            JObject? result = await orgBookService.GetOrgBookQueryAsync(applicant.OrgNumber);
            var orgData = result?.SelectToken("results")?.Children().FirstOrDefault();
            if (orgData == null) return;

            var namesChildren = orgData.SelectToken("names")?.Children();
            if (namesChildren == null) return;

            foreach (var name in namesChildren)
            {
                if (name.SelectToken("type")?.ToString() == "entity_name")
                {
                    string nameText = name.SelectToken("text")?.ToString() ?? string.Empty;
                    double match = nameText.CompareStrings(applicant.ApplicantName ?? string.Empty);
                    applicant.MatchPercentage = (decimal)match;
                    if (applicant.OrgName != nameText)
                    {
                        applicant.OrgName = nameText;
                        await applicantRepository.UpdateAsync(applicant);
                    }
                }
                else if (name.SelectToken("type")?.ToString() == "business_number")
                {
                    string businessNumber = name.SelectToken("text")?.ToString() ?? string.Empty;
                    if (businessNumber != applicant.BusinessNumber)
                    {
                        applicant.BusinessNumber = businessNumber;
                        await applicantRepository.UpdateAsync(applicant);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var ExceptionMessage = ex.Message;
            Logger.LogError(ex, "UpdateApplicantOrgMatchAsync Exception: {ExceptionMessage}", ExceptionMessage);
        }
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
            IndigenousOrgInd = intakeMap.IndigenousOrgInd,
            OrgStatus = intakeMap.OrgStatus,
            RedStop = false,
            FiscalDay = MappingUtil.ConvertToIntFromString(intakeMap.FiscalDay),
            FiscalMonth = intakeMap.FiscalMonth
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