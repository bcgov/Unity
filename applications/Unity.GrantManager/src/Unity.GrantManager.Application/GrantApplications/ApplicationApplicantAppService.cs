using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Flex;
using Unity.Modules.Shared;
using Unity.Modules.Shared.Correlation;
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
        if (application.Applicant is not null && await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Summary.Default))
        {
            applicantInfoDto.ApplicantSummary = ObjectMapper.Map<Applications.Applicant, ApplicantSummaryDto>(application.Applicant);
            applicantInfoDto.ApplicantSummary.FiscalDay = application.Applicant?.FiscalDay.ToString() ?? string.Empty;
        }
        else
        {
            applicantInfoDto.ApplicantSummary = new ApplicantSummaryDto();
        }

        //-- APPLICANT INFO CONTACT
        if (application?.ApplicantAgent is not null && await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Contact.Default))
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
    public async Task<GrantApplicationDto> UpdatePartialApplicantInfoAsync(Guid id, PartialUpdateDto<ApplicantInfoDto> input)
    {
        var application = await applicationRepository.GetAsync(id) ?? throw new EntityNotFoundException();

        // Only update the fields we need to update based on the modified fields
        // This is required to handle controls like the date picker that do not send null values for unchanged fields
        ObjectMapper.Map<ApplicantInfoDto, Application>(input.Data, application);

        // Explicitly handle applicant summary properties with dropdowns that are null but listed in ModifiedFields
        var dtoProperties = typeof(ApplicantSummaryDto).GetProperties();
        var appProperties = typeof(Applicant).GetProperties().ToDictionary(p => p.Name, p => p);

        foreach (var fieldName in input.ModifiedFields)
        {
            if (dtoProperties.FirstOrDefault(p =>
                string.Equals(p.Name, fieldName, StringComparison.OrdinalIgnoreCase)) is { } dtoProperty)
            {
                var value = dtoProperty.GetValue(input.Data);
                if (value == null && appProperties.TryGetValue(dtoProperty.Name, out var appProperty) && appProperty.CanWrite)
                {
                    appProperty.SetValue(application, appProperty.PropertyType.IsValueType
                        && Nullable.GetUnderlyingType(appProperty.PropertyType) == null
                        ? Activator.CreateInstance(appProperty.PropertyType)
                        : null);
                }
            }
        }

        // TODO: Check how deep the mapping is needed here

        // ORGANIZATION INFO
        await UpdateApplicantSummaryInfoAsync(application.ApplicantId, input?.Data.ApplicantSummary);

        // APPLICANT AGENT
        var applicantAgent = await UpsertContactInfoAsync(application, input?.Data.ContactInfo);

        // APPLICANT ADDRESS
        if (input?.Data.ApplicantAddresses != null
            && input.Data.ApplicantAddresses.Count > 0)
        {
            await UpsertApplicantAddresses(application.ApplicantId, input?.Data.ApplicantAddresses);
        }

        // SIGNING AUTHORITY
        if (input?.Data.SigningAuthority is not null)
        {
            ObjectMapper.Map(input?.Data.SigningAuthority, application);
        }

        // CUSTOM FIELDS
        //await PublishCustomFieldUpdatesAsync(application.Id, FlexConsts.ApplicantInfoUiAnchor, input?.CustomFields);
        await applicationRepository.UpdateAsync(application);

        // Add custom worksheet data
        if (input?.Data.CustomFields is not null && input.Data.WorksheetId != Guid.Empty && input.Data.CorrelationId != Guid.Empty)
        {
            await PublishCustomFieldUpdatesAsync(application.Id, FlexConsts.ApplicantInfoUiAnchor, input.Data);
        }

        // TODO: REVIEW
        await applicationRepository.UpdateAsync(application);
        return ObjectMapper.Map<Application, GrantApplicationDto>(application);
    }

    [Authorize(UnitySelector.Applicant.Summary.Update)]
    protected internal async Task<Applicant?> UpdateApplicantSummaryInfoAsync(Guid applicantId, ApplicantSummaryDto? input)
    {
        if (input == null || !await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Summary.Update))
        {
            return null;
        }

        var applicant = await applicantRepository.GetAsync(applicantId)
        ?? throw new EntityNotFoundException();

        ObjectMapper.Map<ApplicantSummaryDto, Applicant>(input, applicant);

        return await applicantRepository.UpdateAsync(applicant);
    }

    [Authorize(UnitySelector.Applicant.Contact.Update)]
    public async Task<ApplicantAgent?> UpsertContactInfoAsync(Application application, ContactInfoDto? input)
    {
        if (input == null)
        {
            return null;
        }

        // TODO Review
        var applicantAgent = await applicantAgentRepository.FirstOrDefaultAsync(a => a.ApplicantId == application.ApplicantId)
        ?? new ApplicantAgent
        {
            ApplicantId   = application.ApplicantId,
            ApplicationId = application.Id
        };

        ObjectMapper.Map<ContactInfoDto, ApplicantAgent>(input, applicantAgent);

        if (applicantAgent.Id == Guid.Empty)
        {
            return await applicantAgentRepository.InsertAsync(applicantAgent);
        }

        return await applicantAgentRepository.UpdateAsync(applicantAgent);
    }

    [Authorize(UnitySelector.Applicant.Location.Update)]
    public async Task UpsertApplicantAddresses(Guid applicantId, List<ApplicantAddressDto> updatedAddresses)
    {
        var applicantAddresses = await applicantAddressRepository.FindByApplicantIdAsync(applicantId);
        foreach (var updatedAddress in updatedAddresses)
        {
            ApplicantAddress? dbAddress = applicantAddresses.FirstOrDefault(a => a.Id == updatedAddress.Id && a.AddressType == updatedAddress.AddressType)
            ?? new ApplicantAddress
            {
                ApplicantId = applicantId,
                AddressType = updatedAddress.AddressType,
            };

            ObjectMapper.Map<ApplicantAddressDto, ApplicantAddress>(updatedAddress);

            if (dbAddress.Id == Guid.Empty)
            {
                await applicantAddressRepository.InsertAsync(dbAddress);
            }
            else
            {
                await applicantAddressRepository.UpdateAsync(dbAddress);
            }
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
}
