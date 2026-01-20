using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Intakes.Mapping;
using Unity.Payments.Events;
using Volo.Abp;
using Unity.GrantManager.Integrations.Orgbook;
using Unity.Modules.Shared;
using Unity.Modules.Shared.Utils;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Integrations.Cas;
using Unity.Payments.Suppliers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Applicants;


[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicantAppService), typeof(IApplicantAppService))]
public class ApplicantAppService(IApplicantRepository applicantRepository,
                                 ISupplierService supplierService,
                                 ISiteAppService siteAppService,
                                 IApplicantAddressRepository addressRepository,
                                 IOrgBookService orgBookService,
                                 IApplicantAgentRepository applicantAgentRepository,
                                 IApplicationRepository applicationRepository) : GrantManagerAppService, IApplicantAppService
{   
    protected new ILogger Logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

    [RemoteService(false)]
    public async Task<Applicant> CreateOrRetrieveApplicantAsync(IntakeMapping intakeMap, Guid applicationId)
    {
        ArgumentNullException.ThrowIfNull(intakeMap);

        Applicant? applicant = await GetExistingApplicantAsync(intakeMap.UnityApplicantId);
        if (applicant == null)
        {
            applicant = await CreateNewApplicantAsync(intakeMap);
        } else {
            applicant.ApplicantName = MappingUtil.ResolveAndTruncateField(600, string.Empty, intakeMap.ApplicantName) ?? applicant.ApplicantName;
            // Intake map uses NonRegisteredBusinessName for non-registered organizations to support legacy mappings
            applicant.NonRegOrgName = intakeMap.NonRegisteredBusinessName ?? applicant.NonRegOrgName;
            applicant.OrgName = intakeMap.OrgName ?? applicant.OrgName;
            applicant.OrgNumber = intakeMap.OrgNumber ?? applicant.OrgNumber;
            applicant.BusinessNumber = intakeMap.BusinessNumber ?? applicant.BusinessNumber;
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

        await CreateApplicantAddressesAsync(intakeMap, applicant, applicationId);
        return applicant;
    }

    [RemoteService(false)]
    public async Task<Applicant> RelateSupplierToApplicant(ApplicantSupplierEto applicantSupplierEto)
    {
        // Validate ApplicantId to ensure it is not Guid.Empty
        if (applicantSupplierEto.ApplicantId == Guid.Empty)
        {
            throw new ArgumentException("ApplicantId cannot be Guid.Empty.", "applicantSupplierEto.ApplicantId");
        }
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
            
            newApplicantAgent.IdentityName = intakeMap.ApplicantAgent?.name ?? "";
            newApplicantAgent.IdentityEmail = intakeMap.ApplicantAgent?.email ?? "";
            
            newApplicantAgent.OidcSubUser = intakeMap.ApplicantAgent?.oidc_sub_user;              
            newApplicantAgent.IdentityProvider = intakeMap.ApplicantAgent?.identity_provider ?? "";
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

    [Authorize(UnitySelector.Applicant.Summary.Update)]
    public async Task<Applicant> PartialUpdateApplicantSummaryAsync(Guid applicantId, PartialUpdateDto<UpdateApplicantSummaryDto> input)
    {
        if (applicantId == Guid.Empty)
        {
            throw new ArgumentException("ApplicantId cannot be empty.", nameof(applicantId));
        }

        ArgumentNullException.ThrowIfNull(input);

        ArgumentNullException.ThrowIfNull(input.Data);

        var applicant = await applicantRepository.GetAsync(applicantId);

        ObjectMapper.Map(input.Data, applicant);

        List<string> modifiedSummaryFields = input.ModifiedFields?
            .Where(field => !string.IsNullOrWhiteSpace(field))
            .Select(field =>
            {
                var segments = field.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 0)
                {
                    throw new InvalidOperationException("Modified field path cannot be empty.");
                }

                return segments[^1];
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        if (modifiedSummaryFields.Contains(nameof(UpdateApplicantSummaryDto.RedStop), StringComparer.OrdinalIgnoreCase))
        {
            applicant.RedStop = input.Data.RedStop;
        }

        if (modifiedSummaryFields.Contains(nameof(UpdateApplicantSummaryDto.IndigenousOrgInd), StringComparer.OrdinalIgnoreCase))
        {
            applicant.IndigenousOrgInd = input.Data.IndigenousOrgInd switch
            {
                true => "Yes",
                false => "No",
                _ => null
            };
        }

        if (modifiedSummaryFields.Count > 0)
        {
            PropertyHelper.ApplyNullValuesFromDto(input.Data, applicant, modifiedSummaryFields);
        }

        return await applicantRepository.UpdateAsync(applicant);
    }

    public async Task UpdateApplicantContactAddressesAsync(Guid applicantId, UpdateApplicantContactAddressesDto input)
    {
        if (applicantId == Guid.Empty)
        {
            throw new ArgumentException("ApplicantId cannot be empty.", nameof(applicantId));
        }

        ArgumentNullException.ThrowIfNull(input);

        if (input.PrimaryContact == null &&
            input.PrimaryPhysicalAddress == null &&
            input.PrimaryMailingAddress == null)
        {
            return;
        }

        if (input.PrimaryContact != null &&
            await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Contact.Update))
        {
            await UpdatePrimaryContactAsync(applicantId, input.PrimaryContact);
        }

        if (input.PrimaryPhysicalAddress != null &&
            await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Location.Update))
        {
            await UpdatePrimaryAddressAsync(applicantId, input.PrimaryPhysicalAddress, GrantApplications.AddressType.PhysicalAddress);
        }

        if (input.PrimaryMailingAddress != null &&
            await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Location.Update))
        {
            await UpdatePrimaryAddressAsync(applicantId, input.PrimaryMailingAddress, GrantApplications.AddressType.MailingAddress);
        }
    }

    private async Task UpdatePrimaryContactAsync(Guid applicantId, UpdatePrimaryContactDto input)
    {
        if (input.Id == Guid.Empty)
        {
            throw new ArgumentException("Contact identifier is required.", nameof(input));
        }

        var applicantAgent = await applicantAgentRepository.GetAsync(input.Id);
        if (applicantAgent.ApplicantId != applicantId)
        {
            throw new BusinessException("Unity:Applicant:ContactNotFound")
                .WithData("ApplicantId", applicantId)
                .WithData("ContactId", input.Id);
        }

        applicantAgent.Name = input.FullName?.Trim() ?? string.Empty;
        applicantAgent.Title = input.Title?.Trim() ?? string.Empty;
        applicantAgent.Email = input.Email?.Trim() ?? string.Empty;
        applicantAgent.Phone = input.BusinessPhone?.Trim() ?? string.Empty;
        applicantAgent.Phone2 = input.CellPhone?.Trim() ?? string.Empty;

        await applicantAgentRepository.UpdateAsync(applicantAgent);
    }

    private async Task UpdatePrimaryAddressAsync(Guid applicantId, UpdatePrimaryApplicantAddressDto input, GrantApplications.AddressType expectedType)
    {
        if (input.Id == Guid.Empty)
        {
            throw new ArgumentException("Address identifier is required.", nameof(input));
        }

        var applicantAddress = await addressRepository.GetAsync(input.Id);

        if (applicantAddress.ApplicantId != applicantId)
        {
            throw new BusinessException("Unity:Applicant:AddressNotFound")
                .WithData("ApplicantId", applicantId)
                .WithData("AddressId", input.Id);
        }

        if (applicantAddress.AddressType != expectedType)
        {
            throw new BusinessException("Unity:Applicant:AddressTypeMismatch")
                .WithData("ApplicantId", applicantId)
                .WithData("AddressId", input.Id)
                .WithData("ExpectedType", expectedType.ToString());
        }

        applicantAddress.Street = input.Street?.Trim() ?? string.Empty;
        applicantAddress.Street2 = input.Street2?.Trim() ?? string.Empty;
        applicantAddress.Unit = input.Unit?.Trim() ?? string.Empty;
        applicantAddress.City = input.City?.Trim() ?? string.Empty;
        applicantAddress.Province = input.Province?.Trim() ?? string.Empty;
        applicantAddress.Postal = input.PostalCode?.Trim() ?? string.Empty;

        await addressRepository.UpdateAsync(applicantAddress);
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
            // Use the built-in System.Text.Json API
            using JsonDocument result = await orgBookService.GetOrgBookAutocompleteQueryAsync(orgbookLookup);
            if (!result.RootElement.TryGetProperty("results", out JsonElement results) ||
                results.GetArrayLength() == 0)
                return applicant;
            JsonElement orgData = results[0];
            await UpdateApplicantOrgNumberAsync(applicant, orgData);
            await UpdateApplicantNamesAsync(applicant, orgData.GetProperty("names").EnumerateArray());
        }
        catch (Exception ex)
        {
            Logger.LogInformation(ex, "UpdateApplicantOrgMatchAsync: Exception: {ExceptionMessage}", ex.Message);
        }
        return applicant;
    }

    private async Task UpdateApplicantOrgNumberAsync(Applicant applicant, JsonElement orgData)
    {
        if (orgData.TryGetProperty("source_id", out JsonElement orgNumberElement) && applicant.OrgNumber == null)
        {
            applicant.OrgNumber = orgNumberElement.GetString();
            await applicantRepository.UpdateAsync(applicant);
        }
    }

    private async Task UpdateApplicantNamesAsync(Applicant applicant, IEnumerable<JsonElement> namesChildren)
    {
        foreach (var name in namesChildren)
        {
            string nameType = name.TryGetProperty("type", out JsonElement typeEl) ? typeEl.GetString() ?? string.Empty : string.Empty;
            string nameText = name.TryGetProperty("text", out JsonElement textEl) ? textEl.GetString() ?? string.Empty : string.Empty;

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
            // Intake map uses NonRegisteredBusinessName for non-registered organizations to support legacy mappings
            NonRegOrgName = intakeMap.NonRegisteredBusinessName,
            OrgName = intakeMap.OrgName,
            OrgNumber = intakeMap.OrgNumber,
            BusinessNumber = intakeMap.BusinessNumber,
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

    private async Task CreateApplicantAddressesAsync(IntakeMapping intakeMap, Applicant applicant, Guid applicationId)
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
                AddressType = AddressType.PhysicalAddress,
                ApplicationId = applicationId
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
                AddressType = AddressType.MailingAddress,
                ApplicationId = applicationId
            });
        }
    }

    public async Task<List<Applicant>> GetApplicantsBySiteIdAsync(Guid siteId)
    {
        List<Applicant> applicants = await applicantRepository.GetApplicantsBySiteIdAsync(siteId);
        return applicants;
    }
    [RemoteService(true)]
    public async Task<JsonDocument> GetApplicantLookUpAutocompleteQueryAsync(string? applicantLookUpQuery)
    {
        JsonDocument result = await applicantRepository.GetApplicantAutocompleteQueryAsync(applicantLookUpQuery);
        return result;
    }

    [RemoteService(true)]
    public async Task UpdateApplicantIdAsync(UpdateApplicantIdDto dto)
    {
        // Validate input
        if (dto == null)
        {
            Logger.LogWarning("UpdateApplicantIdAsync called with null dto.");
            return;
        }

        //Update Application
        var application = await applicationRepository.GetAsync(dto.ApplicationId);
        if (application == null)
        {
            Logger.LogWarning("Application not found for ApplicationId: {ApplicationId}", dto.ApplicationId);
            return;
        }

        var oldApplicantId = application.ApplicantId;
        if (oldApplicantId == dto.ApplicantId)
        {
            Logger.LogInformation("ApplicantId is already set to the requested value. No update required.");
            return;
        }

        application.ApplicantId = dto.ApplicantId;
        await applicationRepository.UpdateAsync(application);

        //Update ApplicationFormSubmissions
        await UpdateApplicationFormSubmissionsAsync(dto.ApplicationId, dto.ApplicantId);

        //Update ApplicantAgent records
        await UpdateApplicantAgentRecordsAsync(oldApplicantId, dto.ApplicantId, dto.ApplicationId);

        //Update ApplicantAddresses records
        await UpdateApplicantAddressRecords(oldApplicantId, dto.ApplicantId, dto.ApplicationId);
    }

    private async Task UpdateApplicantAddressRecords(Guid oldApplicantId, Guid newApplicantId, Guid applicationId)
    {
        try
        {
            List<ApplicantAddress> applicantAddresses = await addressRepository.FindByApplicantIdAndApplicationIdAsync(oldApplicantId, applicationId);
            await UpdateAddress(applicantAddresses, AddressType.MailingAddress, newApplicantId, applicationId);
            await UpdateAddress(applicantAddresses, AddressType.PhysicalAddress, newApplicantId, applicationId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating ApplicantAddress records for ApplicationId: {ApplicationId}", applicationId);
            throw new UserFriendlyException("An error occurred while updating applicant address records.");
        }
    }

    private async Task UpdateAddress(List<ApplicantAddress> applicantAddresses, AddressType applicantAddressType, Guid newApplicantId, Guid applicationId)
    {
        ApplicantAddress? dbAddress = applicantAddresses.Find(address => address.AddressType == applicantAddressType && address.ApplicationId == applicationId);

        if (dbAddress != null)
        {
            dbAddress.ApplicantId = newApplicantId;
            await addressRepository.UpdateAsync(dbAddress);
        }
    }


    [RemoteService(true)]
    public async Task SetDuplicatedAsync(SetApplicantDuplicateDto dto)
    {
        // Set principal as not duplicated
        var principal = await applicantRepository.GetAsync(dto.PrincipalApplicantId);
        if (principal != null && principal.IsDuplicated != false)
        {
            principal.IsDuplicated = false;
            await applicantRepository.UpdateAsync(principal);
        }

        // Set non-principal as duplicated
        var nonPrincipal = await applicantRepository.GetAsync(dto.NonPrincipalApplicantId);
        if (nonPrincipal != null && nonPrincipal.IsDuplicated != true)
        {
            nonPrincipal.IsDuplicated = true;
            await applicantRepository.UpdateAsync(nonPrincipal);
        }
    }

    private async Task UpdateApplicationFormSubmissionsAsync(Guid applicationId, Guid newApplicantId)
    {
        try
        {
            var formSubmissionRepository = LazyServiceProvider.LazyGetRequiredService<IApplicationFormSubmissionRepository>();
            var formSubmissions = await (await formSubmissionRepository.GetQueryableAsync())
                .Where(s => s.ApplicationId == applicationId)
                .ToListAsync();

            foreach (var submission in formSubmissions)
            {
                submission.ApplicantId = newApplicantId;
                await formSubmissionRepository.UpdateAsync(submission);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating ApplicationFormSubmissions for ApplicationId: {ApplicationId}", applicationId);
            throw new UserFriendlyException("An error occurred while updating application form submissions.");
        }
    }

    private async Task UpdateApplicantAgentRecordsAsync(Guid oldApplicantId, Guid newApplicantId, Guid applicationId)
    {
        try
        {
            var agentQueryable = await applicantAgentRepository.GetQueryableAsync();

            var agent = await agentQueryable
                .FirstOrDefaultAsync(a => a.ApplicantId == oldApplicantId && a.ApplicationId == applicationId);

            if (agent != null)
            {
                agent.ApplicantId = newApplicantId;
                await applicantAgentRepository.UpdateAsync(agent);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating ApplicantAgent records for ApplicationId: {ApplicationId}", applicationId);
            throw new UserFriendlyException("An error occurred while updating applicant agent records.");
        }
    }

    [RemoteService(true)]
    public async Task<PagedResultDto<ApplicantListDto>> GetListAsync(ApplicantListRequestDto input)
    {
        var query = await applicantRepository.GetQueryableAsync();

        // Apply default sorting (client-side DataTable handles search, sorting, and paging)
        query = query.OrderByDescending(a => a.CreationTime);

        // Get total count
        var totalCount = await query.CountAsync();

        // Execute query
        var applicants = await query.ToListAsync();

        // Map to DTOs
        var items = applicants.Select(applicant => new ApplicantListDto
            {
                Id = applicant.Id,
                ApplicantName = applicant.ApplicantName,
                UnityApplicantId = applicant.UnityApplicantId,
                OrgName = applicant.OrgName,
                OrgNumber = applicant.OrgNumber,
                OrgStatus = applicant.OrgStatus,
                OrganizationType = applicant.OrganizationType,
                Status = applicant.Status,
                RedStop = applicant.RedStop,
                NonRegisteredBusinessName = applicant.NonRegisteredBusinessName,
                NonRegOrgName = applicant.NonRegOrgName,
                OrganizationSize = applicant.OrganizationSize,
                Sector = applicant.Sector,
                SubSector = applicant.SubSector,
                ApproxNumberOfEmployees = applicant.ApproxNumberOfEmployees,
                IndigenousOrgInd = applicant.IndigenousOrgInd,
                SectorSubSectorIndustryDesc = applicant.SectorSubSectorIndustryDesc,
                FiscalMonth = applicant.FiscalMonth,
                BusinessNumber = applicant.BusinessNumber,
                FiscalDay = applicant.FiscalDay,
                StartedOperatingDate = applicant.StartedOperatingDate.HasValue 
                    ? applicant.StartedOperatingDate.Value.ToDateTime(TimeOnly.MinValue) 
                    : null,
                SupplierId = applicant.SupplierId?.ToString(),
                SiteId = applicant.SiteId,
                MatchPercentage = applicant.MatchPercentage,
                IsDuplicated = applicant.IsDuplicated,
                CreationTime = applicant.CreationTime,
                LastModificationTime = applicant.LastModificationTime
            }).ToList();

        return new PagedResultDto<ApplicantListDto>(totalCount, items);
    }
}
