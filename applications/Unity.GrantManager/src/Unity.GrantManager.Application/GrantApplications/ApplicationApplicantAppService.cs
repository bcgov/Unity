using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Flex;
using Unity.Modules.Shared;
using Unity.Modules.Shared.Correlation;
using Unity.Modules.Shared.Utils;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;

namespace Unity.GrantManager.GrantApplications;

[Authorize]
public class ApplicationApplicantAppService(
    IApplicantRepository applicantRepository,
    IApplicationRepository applicationRepository,
    IApplicantAgentRepository applicantAgentRepository,
    IApplicantAddressRepository applicantAddressRepository,
    ILocalEventBus localEventBus) : GrantManagerAppService, IApplicationApplicantAppService
{
    [Authorize(UnitySelector.Applicant.Default)]
    public async Task<ApplicantInfoDto> GetApplicantInfoTabAsync(Guid applicationId)
    {
        var application = await applicationRepository.WithBasicDetailsAsync(applicationId);
        if (application == null || !await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Default))
        {
            return new ApplicantInfoDto();
        }

        var applicantInfoDto = ObjectMapper.Map<Applications.Application, ApplicantInfoDto>(application);

        applicantInfoDto.ApplicationId = application.Id;
        applicantInfoDto.ApplicantId = application.ApplicantId;
        applicantInfoDto.ApplicationFormId = application.ApplicationFormId;

        applicantInfoDto.ApplicationReferenceNo = application.ReferenceNo;
        applicantInfoDto.ApplicantName = application.Applicant?.ApplicantName ?? string.Empty;

        applicantInfoDto.ApplicationStatusCode = application.ApplicationStatus.StatusCode;
        applicantInfoDto.ElectoralDistrict = application.Applicant?.ElectoralDistrict ?? string.Empty;

        //-- APPLICANT INFO SUMMARY
        if (application.Applicant != null && await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Summary.Default))
        {
            applicantInfoDto.ApplicantSummary = ObjectMapper.Map<Applications.Applicant, ApplicantSummaryDto>(application.Applicant);
            applicantInfoDto.ApplicantSummary.FiscalDay = application.Applicant?.FiscalDay.ToString() ?? string.Empty;
        }
        else
        {
            applicantInfoDto.ApplicantSummary = new ApplicantSummaryDto();
        }

        //-- APPLICANT INFO CONTACT
        if (application?.ApplicantAgent != null && await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Contact.Default))
        {
            applicantInfoDto.ContactInfo = ObjectMapper.Map<ApplicantAgent, ContactInfoDto>(application.ApplicantAgent);
        }
        else
        {
            applicantInfoDto.ContactInfo = new ContactInfoDto();
        }

        //-- SIGNING AUTHORITY
        if (application != null && await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Authority.Default))
        {
            applicantInfoDto.SigningAuthority = ObjectMapper.Map<Applications.Application, SigningAuthorityDto>(application);
        }
        else
        {
            applicantInfoDto.SigningAuthority = new SigningAuthorityDto();
        }

        //-- APPLICANT INFO ADDRESS
        if (await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Location.Default))
        {
            applicantInfoDto.ApplicantAddresses = ObjectMapper.Map<List<ApplicantAddress>, List<ApplicantAddressDto>>(application?.Applicant?.ApplicantAddresses?.ToList() ?? []);
        }
        else
        {
            applicantInfoDto.ApplicantAddresses = [];
        }

        return applicantInfoDto;
    }

    [Obsolete("Use GetApplicantInfoTabAsync instead.")]
    [Authorize]
    public async Task<ApplicationApplicantInfoDto> GetByApplicationIdAsync(Guid applicationId)
    {
        var applicantInfo = await applicationRepository.WithBasicDetailsAsync(applicationId);
        if (applicantInfo == null) return new ApplicationApplicantInfoDto();

        return new ApplicationApplicantInfoDto()
        {
            ApplicantId = applicantInfo.Applicant.Id,
            ApplicationFormId = applicantInfo.ApplicationFormId,
            ApplicantName = applicantInfo.Applicant?.ApplicantName ?? string.Empty,
            ApplicationReferenceNo = applicantInfo.ReferenceNo,
            ApplicationStatus = applicantInfo.ApplicationStatus.InternalStatus,
            ApplicationStatusCode = applicantInfo.ApplicationStatus.StatusCode,

            OrganizationName = applicantInfo.Applicant?.OrgName ?? string.Empty,
            OrganizationSize = applicantInfo.Applicant?.OrganizationSize ?? string.Empty,
            OrganizationType = applicantInfo.Applicant?.OrganizationType ?? string.Empty,
            OrgNumber = applicantInfo.Applicant?.OrgNumber ?? string.Empty,
            OrgStatus = applicantInfo.Applicant?.OrgStatus ?? string.Empty,
            NonRegOrgName = applicantInfo.Applicant?.NonRegOrgName ?? string.Empty,

            Sector = applicantInfo.Applicant?.Sector ?? string.Empty,
            SectorSubSectorIndustryDesc = applicantInfo.Applicant?.SectorSubSectorIndustryDesc ?? string.Empty,
            SubSector = applicantInfo.Applicant?.SubSector ?? string.Empty,
            RedStop = applicantInfo.Applicant?.RedStop ?? false,
            IndigenousOrgInd = applicantInfo.Applicant?.IndigenousOrgInd ?? string.Empty,
            UnityApplicantId = applicantInfo.Applicant?.UnityApplicantId ?? string.Empty,
            FiscalDay = applicantInfo.Applicant?.FiscalDay.ToString() ?? string.Empty,
            FiscalMonth = applicantInfo.Applicant?.FiscalMonth ?? string.Empty,

            SigningAuthorityBusinessPhone = applicantInfo.SigningAuthorityBusinessPhone ?? string.Empty,
            SigningAuthorityCellPhone = applicantInfo.SigningAuthorityCellPhone ?? string.Empty,
            SigningAuthorityEmail = applicantInfo.SigningAuthorityEmail ?? string.Empty,
            SigningAuthorityFullName = applicantInfo.SigningAuthorityFullName ?? string.Empty,
            SigningAuthorityTitle = applicantInfo.SigningAuthorityTitle ?? string.Empty,

            ContactFullName = applicantInfo.ApplicantAgent?.Name ?? string.Empty,
            ContactTitle = applicantInfo.ApplicantAgent?.Title ?? string.Empty,
            ContactEmail = applicantInfo.ApplicantAgent?.Email ?? string.Empty,
            ContactBusinessPhone = applicantInfo.ApplicantAgent?.Phone ?? string.Empty,
            ContactCellPhone = applicantInfo.ApplicantAgent?.Phone2 ?? string.Empty,

            ApplicantAddresses = ObjectMapper.Map<List<ApplicantAddress>, List<ApplicantAddressDto>>(applicantInfo.Applicant?.ApplicantAddresses?.ToList() ?? []),
            ElectoralDistrict = applicantInfo.Applicant?.ElectoralDistrict ?? string.Empty
        };
    }

    [Authorize(UnitySelector.Applicant.UpdatePolicy)]
    public async Task<GrantApplicationDto> UpdatePartialApplicantInfoAsync(Guid applicationId, PartialUpdateDto<UpdateApplicantInfoDto> input)
    {
        var application = await applicationRepository.GetAsync(applicationId) ?? throw new EntityNotFoundException();

        if (input == null || input.Data == null)
        {
            throw new ArgumentNullException(nameof(input), "Input data cannot be null.");
        }

        // Only update the fields we need to update based on the modified
        ObjectMapper.Map<UpdateApplicantInfoDto, Applications.Application>(input.Data, application);

        //-- APPLICANT INFO - SUMMARY
        if (input.Data.ApplicantSummary != null
            && await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Summary.Update))
        {
            await InternalPartialUpdateApplicantSummaryInfoAsync(application.Applicant, input.Data.ApplicantSummary, input.ModifiedFields);
        }

        //-- APPLICANT INFO - CONTACT (APPLICANT AGENT)
        if (input.Data.ContactInfo != null
            && await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Contact.Update))
        {
            await CreateOrUpdateContactInfoAsync(applicationId, application.ApplicantId, input.Data.ContactInfo);
        }

        //-- APPLICANT INFO - SIGNING AUTHORITY (APPLICATION)
        if (input.Data.SigningAuthority != null
            && await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Authority.Update))
        {
            // Move to applicaiton service
            ObjectMapper.Map(input.Data.SigningAuthority, application);
        }

        //-- APPLICANT INFO - ADDRESS
        if (input.Data.PhysicalAddress != null
            && await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Location.Update))
        {
            input.Data.PhysicalAddress.AddressType = AddressType.PhysicalAddress;
            await CreateOrUpdateApplicantAddress(applicationId, application.ApplicantId, input.Data.PhysicalAddress);
        }

        if (input.Data.MailingAddress != null
            && await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Location.Update))
        {
            input.Data.MailingAddress.AddressType = AddressType.MailingAddress;
            await CreateOrUpdateApplicantAddress(applicationId, application.ApplicantId, input.Data.MailingAddress);
        }

        //-- APPLICANT INFO CUSTOM FIELDS
        if (input.Data.CustomFields?.ValueKind != JsonValueKind.Null && input.Data.CorrelationId != Guid.Empty)
        {
            // Handle multiple worksheets
            if (input.Data.WorksheetIds?.Count > 0)
            {
                foreach (var worksheetId in input.Data.WorksheetIds)
                {
                    var worksheetCustomFields = ExtractCustomFieldsForWorksheet(input.Data.CustomFields, worksheetId);
                    if (worksheetCustomFields.Count > 0)
                    {
                        var worksheetData = new CustomDataFieldDto
                        {
                            WorksheetId = worksheetId,
                            CustomFields = worksheetCustomFields,
                            CorrelationId = input.Data.CorrelationId
                        };
                        await PublishCustomFieldUpdatesAsync(application.Id, FlexConsts.ApplicantInfoUiAnchor, worksheetData);
                    }
                }
            }
            // Fallback for single worksheet (backward compatibility)
            else if (input.Data.WorksheetId != Guid.Empty)
            {
                await PublishCustomFieldUpdatesAsync(application.Id, FlexConsts.ApplicantInfoUiAnchor, input.Data);
            }
        }

        var updatedApplication = await applicationRepository.UpdateAsync(application);
        return ObjectMapper.Map<Applications.Application, GrantApplicationDto>(updatedApplication);
    }

    /// <summary>
    /// Updates the Applicant Summary information for the given applicant while ignoring null values unless explicitly specified in modifiedFields.
    /// </summary>
    /// <param name="applicantId"></param>
    /// <param name="applicantSummary"></param>
    /// <param name="modifiedFields"></param>
    /// <returns></returns>
    /// <exception cref="EntityNotFoundException"></exception>
    [Authorize(UnitySelector.Applicant.Summary.Update)]
    protected internal async Task<Applications.Applicant> PartialUpdateApplicantSummaryInfoAsync(Guid applicantId, UpdateApplicantSummaryDto applicantSummary, List<string>? modifiedFields = default)
    {
        var applicant = await applicantRepository.GetAsync(applicantId) ?? throw new EntityNotFoundException();
        return await InternalPartialUpdateApplicantSummaryInfoAsync(applicant, applicantSummary, modifiedFields);
    }

    /// <summary>
    /// Updates the Applicant Summary information for the given applicant while ignoring null values unless explicitly specified in modifiedFields.
    /// </summary>
    /// <param name="applicantId"></param>
    /// <param name="applicantSummary"></param>
    /// <param name="modifiedFields"></param>
    /// <returns></returns>
    /// <exception cref="EntityNotFoundException"></exception>
    private async Task<Applications.Applicant> InternalPartialUpdateApplicantSummaryInfoAsync(Applications.Applicant applicant, UpdateApplicantSummaryDto applicantSummary, List<string>? modifiedFields = default)
    {
        ObjectMapper.Map<UpdateApplicantSummaryDto, Applications.Applicant>(applicantSummary, applicant);

        var modifiedSummaryFields = modifiedFields?
                .Where(f => f.StartsWith("ApplicantSummary.", StringComparison.Ordinal))
                .Select(f => f["ApplicantSummary.".Length..]).ToList() ?? [];

        if (modifiedSummaryFields != null && modifiedSummaryFields.Count > 0) // Ensure modifiedFields is not null
        {
            // Handle null values for changed fields
            PropertyHelper.ApplyNullValuesFromDto(
                applicantSummary,
                applicant,
                modifiedSummaryFields ?? []); // Provide a fallback for null
        }

        return await applicantRepository.UpdateAsync(applicant);
    }

    /// <summary>
    /// Creates or updates the appicant agent (contact info) for the given applicant. Ignores null values unless explicitly specified in modifiedFields.
    /// </summary>
    /// <param name="applicantId"></param>
    /// <param name="contactInfo"></param>
    /// <returns></returns>
    [Authorize(UnitySelector.Applicant.Contact.Update)]
    protected internal async Task<ApplicantAgent?> CreateOrUpdateContactInfoAsync(Guid applicationId, Guid applicantId, ContactInfoDto contactInfo)
    {
        var applicantAgent = await applicantAgentRepository.FirstOrDefaultAsync(a => a.ApplicantId == applicantId && a.ApplicationId == applicationId)
        ?? new ApplicantAgent
        {
            ApplicantId   = applicantId,
            ApplicationId = applicationId,
        };

        ObjectMapper.Map<ContactInfoDto, ApplicantAgent>(contactInfo, applicantAgent);

        if (applicantAgent.Id == Guid.Empty)
        {
            return await applicantAgentRepository.InsertAsync(applicantAgent);
        }
        else
        {
            return await applicantAgentRepository.UpdateAsync(applicantAgent);
        }
    }

    /// <summary>
    /// Creates or updates the applicant addresses for the given applicant. Ignores null values unless explicitly specified in modifiedFields. 
    /// </summary>
    /// <param name="applicantId"></param>
    /// <param name="applicantAddress"></param>
    /// <param name="modifiedFields"></param>
    /// <returns></returns>
    [Authorize(UnitySelector.Applicant.Location.Update)]
    protected internal async Task CreateOrUpdateApplicantAddress(Guid applicationId, Guid applicantId, UpdateApplicantAddressDto updatedAddress)
    {
        var applicantAddresses = await applicantAddressRepository.FindByApplicantIdAndApplicationIdAsync(applicantId, applicationId);

        ApplicantAddress? dbAddress = applicantAddresses.FirstOrDefault(a => a.AddressType == updatedAddress.AddressType)
        ?? new ApplicantAddress
        {
            ApplicantId = applicantId,
            AddressType = updatedAddress.AddressType,
            ApplicationId = applicationId,
        };

        ObjectMapper.Map<UpdateApplicantAddressDto, ApplicantAddress>(updatedAddress, dbAddress);

        if (dbAddress.Id == Guid.Empty)
        {
            await applicantAddressRepository.InsertAsync(dbAddress);
        }
        else
        {
            await applicantAddressRepository.UpdateAsync(dbAddress);
        }
    }

    protected virtual async Task PublishCustomFieldUpdatesAsync(Guid applicationId, string uiAnchor, CustomDataFieldDto input)
    {
        if (await FeatureChecker.IsEnabledAsync("Unity.Flex"))
        {
            if (input.CorrelationId != Guid.Empty)
            {
                await localEventBus.PublishAsync(new PersistWorksheetIntanceValuesEto()
                {
                    InstanceCorrelationId = applicationId,
                    InstanceCorrelationProvider = CorrelationConsts.Application,
                    SheetCorrelationId = input.CorrelationId,
                    SheetCorrelationProvider = CorrelationConsts.FormVersion,
                    UiAnchor = uiAnchor,
                    CustomFields = input.CustomFields,
                    WorksheetId = input.WorksheetId
                });
            }
            else
            {
                Logger.LogError("Unable to resolve for version");
            }
        }
    }

    public async Task<bool> GetSupplierNameMatchesCheck(Guid applicantId, string? supplierName)
    {
        if (string.IsNullOrWhiteSpace(supplierName))
        {
            return true; // If supplierName is null or empty, there is nothing to warn about
        }

        var applicant = await applicantRepository.GetAsync(applicantId) ?? throw new EntityNotFoundException();

        var normalizedSupplierName = supplierName?.Trim();
        var organizationName = applicant.OrgName?.Trim();
        var nonRegisteredOrganizationName = applicant.NonRegOrgName?.Trim();

        // Match if either orgName or nonRegisteredOrgName matches supplierName
        // - If both orgName and nonRegisteredOrgName are null or empty, return true
        // - Otherwise, return true if supplierName matches either orgName or nonRegisteredOrgName (case-insensitive)
        if (string.IsNullOrEmpty(organizationName) && string.IsNullOrEmpty(nonRegisteredOrganizationName))
        {
            return true;
        }

        return string.Equals(normalizedSupplierName, organizationName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(normalizedSupplierName, nonRegisteredOrganizationName, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, object> ExtractCustomFieldsForWorksheet(JsonElement customFields, Guid worksheetId)
    {
        var result = new Dictionary<string, object>();
        var worksheetSuffix = $".{worksheetId}";
        
        foreach (var property in customFields.EnumerateObject())
        {
            if (property.Name.EndsWith(worksheetSuffix))
            {
                // Remove worksheet ID suffix to get original field name
                var originalFieldName = property.Name.Substring(0, property.Name.Length - worksheetSuffix.Length);
                result[originalFieldName] = property.Value.GetRawText();
            }
        }
        
        return result;
    }
}
