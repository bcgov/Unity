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

        var applicantInfoDto = ObjectMapper.Map<Application, ApplicantInfoDto>(application);

        applicantInfoDto.ApplicationId = application.Id;
        applicantInfoDto.ApplicantId = application.ApplicantId;
        applicantInfoDto.ApplicationFormId = application.ApplicationFormId;
        applicantInfoDto.ApplicationReferenceNo = application.ReferenceNo;
        applicantInfoDto.ApplicantName = application.Applicant?.ApplicantName ?? string.Empty;
        applicantInfoDto.ApplicationStatusCode = application.ApplicationStatus.StatusCode;
        applicantInfoDto.ElectoralDistrict = application.Applicant?.ElectoralDistrict ?? string.Empty;

        //-- APPLICANT INFO SUMMARY
        if (application.Applicant != null &&
            await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Summary.Default))
        {
            applicantInfoDto.ApplicantSummary = ObjectMapper.Map<Applications.Applicant, ApplicantSummaryDto>(application.Applicant);
            applicantInfoDto.ApplicantSummary.FiscalDay = application.Applicant?.FiscalDay.ToString() ?? string.Empty;
        }
        else
        {
            applicantInfoDto.ApplicantSummary = new ApplicantSummaryDto();
        }

        //-- APPLICANT INFO CONTACT
        if (application?.ApplicantAgent != null &&
            await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Contact.Default))
        {
            applicantInfoDto.ContactInfo = ObjectMapper.Map<ApplicantAgent, ContactInfoDto>(application.ApplicantAgent);
        }
        else
        {
            applicantInfoDto.ContactInfo = new ContactInfoDto();
        }

        //-- SIGNING AUTHORITY
        if (application != null &&
            await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Authority.Default))
        {
            applicantInfoDto.SigningAuthority = ObjectMapper.Map<Application, SigningAuthorityDto>(application);
        }
        else
        {
            applicantInfoDto.SigningAuthority = new SigningAuthorityDto();
        }

        //-- APPLICANT INFO ADDRESS
        if (await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Location.Default))
        {
            applicantInfoDto.ApplicantAddresses =
                ObjectMapper.Map<List<ApplicantAddress>, List<ApplicantAddressDto>>(application?.Applicant?.ApplicantAddresses?.ToList() ?? []);
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
            BusinessNumber = applicantInfo.Applicant?.BusinessNumber ?? string.Empty,
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
    public async Task<GrantApplicationDto> UpdatePartialApplicantInfoAsync(
        Guid applicationId,
        PartialUpdateDto<UpdateApplicantInfoDto> input)
    {
        if (input?.Data == null)
            throw new ArgumentNullException(nameof(input), "Input data cannot be null.");

        var application = await applicationRepository.GetAsync(applicationId)
            ?? throw new EntityNotFoundException();

        // Map standard fields
        ObjectMapper.Map(input.Data, application);

        // Applicant summary
        if (input.Data.ApplicantSummary != null &&
            await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Summary.Update))
        {
            await InternalPartialUpdateApplicantSummaryInfoAsync(
                application.Applicant,
                input.Data.ApplicantSummary,
                input.ModifiedFields
            );
        }

        // Contact info
        if (input.Data.ContactInfo != null &&
            await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Contact.Update))
        {
            await CreateOrUpdateContactInfoAsync(applicationId, application.ApplicantId, input.Data.ContactInfo);
        }

        // Signing authority
        if (input.Data.SigningAuthority != null &&
            await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Authority.Update))
        {
            ObjectMapper.Map(input.Data.SigningAuthority, application);
        }

        // Addresses
        await UpdateAddressIfPresent(applicationId, application.ApplicantId, input.Data.PhysicalAddress, AddressType.PhysicalAddress);
        await UpdateAddressIfPresent(applicationId, application.ApplicantId, input.Data.MailingAddress, AddressType.MailingAddress);

        // Electoral district
        if (input.Data.ElectoralDistrict != null &&
            await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Location.Update) &&
            application.Applicant != null)
        {
            application.Applicant.ElectoralDistrict = input.Data.ElectoralDistrict;
        }

        // Custom fields
        if (HasValue(input.Data.CustomFields) && input.Data.CorrelationId != Guid.Empty)
        {
            await HandleCustomFieldsAsync(application.Id, input.Data);
        }

        var updatedApplication = await applicationRepository.UpdateAsync(application);
        return ObjectMapper.Map<Application, GrantApplicationDto>(updatedApplication);
    }

    private async Task UpdateAddressIfPresent(Guid applicationId, Guid applicantId, UpdateApplicantAddressDto? address, AddressType type)
    {
        if (address == null) return;

        if (await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Location.Update))
        {
            address.AddressType = type;
            await CreateOrUpdateApplicantAddress(applicationId, applicantId, address);
        }
    }

    private async Task HandleCustomFieldsAsync(Guid applicationId, UpdateApplicantInfoDto data)
    {
        if (data.WorksheetIds?.Count > 0)
        {
            foreach (var worksheetId in data.WorksheetIds)
            {
                var worksheetCustomFields = ExtractCustomFieldsForWorksheet(data.CustomFields, worksheetId);
                if (worksheetCustomFields.Count == 0) continue;

                var worksheetData = new CustomDataFieldDto
                {
                    WorksheetId = worksheetId,
                    CustomFields = worksheetCustomFields,
                    CorrelationId = data.CorrelationId
                };

                await PublishCustomFieldUpdatesAsync(applicationId, FlexConsts.ApplicantInfoUiAnchor, worksheetData);
            }
        }
        else if (data.WorksheetId != Guid.Empty)
        {
            await PublishCustomFieldUpdatesAsync(applicationId, FlexConsts.ApplicantInfoUiAnchor, data);
        }
    }

    private static bool HasValue(object? value)
    {
        return value switch
        {
            null => false,
            string s => !string.IsNullOrWhiteSpace(s),
            JsonElement element => element.ValueKind switch
            {
                JsonValueKind.Object => element.EnumerateObject().Any(),
                JsonValueKind.Array => element.EnumerateArray().Any(),
                JsonValueKind.String => !string.IsNullOrWhiteSpace(element.GetString()),
                JsonValueKind.Number => true,
                JsonValueKind.True => true,
                JsonValueKind.False => true,
                _ => false
            },
            _ => true // any other non-null object (int, bool, DTO, etc.)
        };
    }

    [Authorize(UnitySelector.Applicant.Summary.Update)]
    protected internal async Task<Applicant> PartialUpdateApplicantSummaryInfoAsync(
        Guid applicantId,
        UpdateApplicantSummaryDto applicantSummary,
        List<string>? modifiedFields = default)
    {
        var applicant = await applicantRepository.GetAsync(applicantId) ?? throw new EntityNotFoundException();
        return await InternalPartialUpdateApplicantSummaryInfoAsync(applicant, applicantSummary, modifiedFields);
    }

    private async Task<Applications.Applicant> InternalPartialUpdateApplicantSummaryInfoAsync(
        Applications.Applicant applicant,
        UpdateApplicantSummaryDto applicantSummary,
        List<string>? modifiedFields = default)
    {
        ObjectMapper.Map<UpdateApplicantSummaryDto, Applications.Applicant>(applicantSummary, applicant);

        var modifiedSummaryFields = modifiedFields?
            .Where(f => f.StartsWith("ApplicantSummary.", StringComparison.Ordinal))
            .Select(f => f["ApplicantSummary.".Length..]).ToList() ?? [];

        if (modifiedSummaryFields.Count > 0)
        {
            PropertyHelper.ApplyNullValuesFromDto(applicantSummary, applicant, modifiedSummaryFields);
        }

        return await applicantRepository.UpdateAsync(applicant);
    }

    [Authorize(UnitySelector.Applicant.Contact.Update)]
    protected internal async Task<ApplicantAgent?> CreateOrUpdateContactInfoAsync(Guid applicationId, Guid applicantId, ContactInfoDto contactInfo)
    {
        var applicantAgent = await applicantAgentRepository.FirstOrDefaultAsync(a =>
            a.ApplicantId == applicantId && a.ApplicationId == applicationId)
            ?? new ApplicantAgent { ApplicantId = applicantId, ApplicationId = applicationId };

        ObjectMapper.Map(contactInfo, applicantAgent);

        return applicantAgent.Id == Guid.Empty
            ? await applicantAgentRepository.InsertAsync(applicantAgent)
            : await applicantAgentRepository.UpdateAsync(applicantAgent);
    }

    [Authorize(UnitySelector.Applicant.Location.Update)]
    protected internal async Task CreateOrUpdateApplicantAddress(Guid applicationId, Guid applicantId, UpdateApplicantAddressDto updatedAddress)
    {
        var applicantAddresses = await applicantAddressRepository.FindByApplicantIdAndApplicationIdAsync(applicantId, applicationId);

        var dbAddress = applicantAddresses.FirstOrDefault(a => a.AddressType == updatedAddress.AddressType)
            ?? new ApplicantAddress { ApplicantId = applicantId, AddressType = updatedAddress.AddressType, ApplicationId = applicationId };

        ObjectMapper.Map(updatedAddress, dbAddress);

        if (dbAddress.Id == Guid.Empty)
            await applicantAddressRepository.InsertAsync(dbAddress);
        else
            await applicantAddressRepository.UpdateAsync(dbAddress);
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
        if (string.IsNullOrWhiteSpace(supplierName)) return true;

        var applicant = await applicantRepository.GetAsync(applicantId) ?? throw new EntityNotFoundException();
        var normalizedSupplierName = supplierName?.Trim();
        var organizationName = applicant.OrgName?.Trim();
        var nonRegisteredOrganizationName = applicant.NonRegOrgName?.Trim();

        if (string.IsNullOrEmpty(organizationName) && string.IsNullOrEmpty(nonRegisteredOrganizationName))
        {
            return true;
        }

        return string.Equals(normalizedSupplierName, organizationName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedSupplierName, nonRegisteredOrganizationName, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, object> ExtractCustomFieldsForWorksheet(JsonElement customFields, Guid worksheetId)
    {
        var worksheetSuffix = $".{worksheetId}";

        return customFields.EnumerateObject()
            .Where(property => property.Name.EndsWith(worksheetSuffix))
            .ToDictionary(
                property => property.Name[..^worksheetSuffix.Length],
                property => property.Value.ValueKind == JsonValueKind.String
                    ? (object)property.Value.GetString()!
                    : string.Empty
            );
    }
}
