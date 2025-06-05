using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Intakes.Mapping;
using Unity.GrantManager.Integration.Orgbook;
using Unity.Modules.Shared.Utils;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Events;
using Unity.Payments.Integrations.Cas;
using Unity.Payments.Suppliers;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Applicants;


[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicantAppService), typeof(IApplicantAppService))]
public class ApplicantAppService(IApplicantRepository applicantRepository,
                                 ISupplierService supplierService,
                                 ISiteAppService siteAppService,
                                 IApplicantAddressRepository addressRepository,
                                 IOrgBookService orgBookService,
                                 IApplicantAgentRepository applicantAgentRepository) : GrantManagerAppService, IApplicantAppService
{   
    protected new ILogger Logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

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
        applicant.SiteId = null; // Reset site id to null
        // lookup sites if there is only one then set it as default
        if (applicant.SupplierId != null)
        {
            List<Site> sites = await siteAppService.GetSitesBySupplierIdAsync(applicant.SupplierId.Value);
            if (sites.Count == 1)
            {
                applicant.SiteId = sites.FirstOrDefault()?.Id;
            }
        }

        await applicantRepository.UpdateAsync(applicant);
        return applicant;
    }

    [RemoteService(false)]
    public async Task<ApplicantAgent> CreateApplicantAgentAsync(ApplicantAgentDto applicantAgentDto)
    {
        var applicant = applicantAgentDto.Applicant;
        var application = applicantAgentDto.Application;
        var intakeMap = applicantAgentDto.IntakeMap;
        var applicantAgent = await applicantAgentRepository.GetByApplicantIdAsync(applicant.Id);

        var newApplicantAgent = new ApplicantAgent
        {
            ApplicantId = applicant.Id,
            ApplicationId = application.Id
        };

        if (applicantAgent != null)
        {
            newApplicantAgent.Name = intakeMap.ContactName ?? applicantAgent.Name;
            newApplicantAgent.Phone = intakeMap.ContactPhone ?? applicantAgent.Phone;
            newApplicantAgent.Phone2 = intakeMap.ContactPhone2 ?? applicantAgent.Phone2;
            newApplicantAgent.Email = intakeMap.ContactEmail ?? applicantAgent.Email;
            newApplicantAgent.Title = intakeMap.ContactTitle ?? applicantAgent.Title;
        }
        else
        {
            newApplicantAgent.Name = intakeMap.ContactName ?? string.Empty;
            newApplicantAgent.Phone = intakeMap.ContactPhone ?? string.Empty;
            newApplicantAgent.Phone2 = intakeMap.ContactPhone2 ?? string.Empty;
            newApplicantAgent.Email = intakeMap.ContactEmail ?? string.Empty;
            newApplicantAgent.Title = intakeMap.ContactTitle ?? string.Empty;
        }

        if (MappingUtil.IsJObject(intakeMap.ApplicantAgent))
        {
            newApplicantAgent.BceidUserGuid = intakeMap.ApplicantAgent?.bceid_user_guid ?? Guid.Empty;
            newApplicantAgent.BceidBusinessGuid = intakeMap.ApplicantAgent?.bceid_business_guid ?? Guid.Empty;
            newApplicantAgent.BceidBusinessName = intakeMap.ApplicantAgent?.bceid_business_name ?? "";
            newApplicantAgent.BceidUserName = intakeMap.ApplicantAgent?.bceid_username ?? "";
            newApplicantAgent.IdentityProvider = intakeMap.ApplicantAgent?.identity_provider ?? "";
            newApplicantAgent.IdentityName = intakeMap.ApplicantAgent?.name ?? "";
            newApplicantAgent.IdentityEmail = intakeMap.ApplicantAgent?.email ?? "";
        }

        await applicantAgentRepository.InsertAsync(newApplicantAgent);
        return newApplicantAgent;
    }
    
    public async Task RelateDefaultSupplierAsync(ApplicantAgentDto applicantAgentDto) {
        var applicant = applicantAgentDto.Applicant;

        if (applicant.BusinessNumber == null 
            && applicant.MatchPercentage == null 
            && !string.IsNullOrEmpty(applicant.OrgNumber)
            && !applicant.OrgNumber.Contains("No Data", StringComparison.OrdinalIgnoreCase))
        {
            applicant = await UpdateApplicantOrgMatchAsync(applicant);
        }

        if (applicant.SupplierId != null) return;

        if(applicant.BusinessNumber != null) {
            // This fires a detached process event which may update the supplier if it finds it in CAS via the BN9
            await supplierService.UpdateApplicantSupplierInfoByBn9(applicant.BusinessNumber, applicant.Id);
        }
    }

    [RemoteService(true)]
    public async Task MatchApplicantOrgNamesAsync()
    {
        List<Applicant> applicants = await applicantRepository.GetUnmatchedApplicantsAsync();
        foreach (Applicant applicant in applicants)
        {
            // Create match lookups for applicants with org numbers but no org names
            if (applicant.OrgNumber.IsNullOrEmpty() || !applicant.OrgName.IsNullOrEmpty()) continue;
            await UpdateApplicantOrgMatchAsync(applicant);
        }
    }

    [RemoteService(true)]
    public async Task<int> GetNextUnityApplicantIdAsync()
    {
        // Finds the first available Unity Applicant ID, starting from 100000.

        var applicantQuery = await applicantRepository.GetQueryableAsync();

        // Fetch non-null UnityApplicantId strings from the database.
        var allUnityIdStrings = await applicantQuery
            .Where(a => a.UnityApplicantId != null)
            .Select(a => a.UnityApplicantId)
            .ToListAsync();

        var relevantUnityIds = await applicantQuery
            .Where(a => a.UnityApplicantId != null)
            .Select(a => new { UnityApplicantId = a.UnityApplicantId, ParsedId = (int?)null })
            .ToListAsync();

        var updatedRelevantUnityIds = relevantUnityIds
            .Select(item =>
            {
                if (int.TryParse(item.UnityApplicantId, out var parsedId) && parsedId >= 100000)
                {
                    return new { UnityApplicantId = item.UnityApplicantId ?? string.Empty, ParsedId = (int?)parsedId };
                }
                return new { UnityApplicantId = item.UnityApplicantId ?? string.Empty, ParsedId = item.ParsedId };
            })
            .ToList();

        var orderedIds = updatedRelevantUnityIds
            .Where(a => a.ParsedId.HasValue)
            .Select(a => a.ParsedId!.Value) // Use the null-forgiving operator (!) to assert that ParsedId is not null
            .OrderBy(id => id)
            .ToList();

        int candidate = 100000; // Starting ID for availability search.

        foreach (var id in orderedIds)
        {
            if (id == candidate)
            {
                candidate++;
            }
            else // Gap found: candidate is the first available ID.
            {
                break;
            }
        }

        return candidate;
    }

    [RemoteService(true)]
    public async Task<Applicant?> GetExistingApplicantAsync(string? unityApplicantId)
    {
        if (unityApplicantId.IsNullOrEmpty()) return null;
        return await applicantRepository.GetByUnityApplicantIdAsync(unityApplicantId);
    }

    public async Task<Applicant> UpdateApplicantOrgMatchAsync(Applicant applicant)
    {
        try
        {
            string? orgbookLookup = string.IsNullOrEmpty(applicant.OrgNumber) ? applicant.ApplicantName : applicant.OrgNumber;
            if (string.IsNullOrEmpty(orgbookLookup)) return applicant;

            JObject? result = await orgBookService.GetOrgBookQueryAsync(orgbookLookup);
            var orgData = result?.SelectToken("results")?.Children().FirstOrDefault();
            if (orgData == null) return applicant;

            await UpdateApplicantOrgNumberAsync(applicant, orgData);
            await UpdateApplicantNamesAsync(applicant, orgData.SelectToken("names")?.Children());
        }
        catch (Exception ex)
        {
            Logger.LogInformation(ex, "UpdateApplicantOrgMatchAsync: Exception: {ExceptionMessage}", ex.Message);
        }

        return applicant;
    }

    private async Task UpdateApplicantOrgNumberAsync(Applicant applicant, JToken orgData)
    {
        var orgNumber = orgData.SelectToken("source_id");
        if (applicant.OrgNumber == null && orgNumber != null)
        {
            applicant.OrgNumber = orgNumber.ToString();
            await applicantRepository.UpdateAsync(applicant);
        }
    }

    private async Task UpdateApplicantNamesAsync(Applicant applicant, IEnumerable<JToken>? namesChildren)
    {
        if (namesChildren == null) return;

        foreach (var name in namesChildren)
        {
            string nameType = name.SelectToken("type")?.ToString() ?? string.Empty;
            string nameText = name.SelectToken("text")?.ToString() ?? string.Empty;

            if (nameType == "entity_name")
            {
                double match = nameText.CompareStrings(applicant.ApplicantName ?? string.Empty);
                applicant.MatchPercentage = (decimal)match;
                if (applicant.OrgName != nameText)
                {
                    applicant.OrgName = nameText;
                    await applicantRepository.UpdateAsync(applicant);
                }
            }
            else if (nameType == "business_number" && nameText != applicant.BusinessNumber)
            {
                applicant.BusinessNumber = nameText;
                await applicantRepository.UpdateAsync(applicant);
            }
        }
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

    public async Task<List<Applicant>> GetApplicantsBySiteIdAsync(Guid siteId)
    {
        List<Applicant> applicants = await applicantRepository.GetApplicantsBySiteIdAsync(siteId);
        return applicants;
    }

}