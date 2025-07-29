using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Locality;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo;


[Widget(
    RefreshUrl = "Widget/ApplicantInfo/Refresh",
    ScriptTypes = [typeof(ApplicantInfoScriptBundleContributor)],
    StyleTypes = [typeof(ApplicantInfoStyleBundleContributor)],
    AutoInitialize = true)]
public class ApplicantInfoViewComponent(
    IApplicationApplicantAppService applicationAppicantService,
    ISectorService applicationSectorAppService,
    IElectoralDistrictService applicationElectoralDistrictAppService,
    IApplicationFormAppService applicationFormAppService) : AbpViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(Guid applicationId, Guid applicationFormVersionId)
    {
        var applicantInfoDto = await applicationAppicantService.GetApplicantInfoTabAsync(applicationId);
        var electoralDistrictAddressType = await applicationFormAppService.GetElectoralDistrictAddressTypeAsync(applicantInfoDto.ApplicationFormId);

        if (applicantInfoDto == null)
        {
            throw new InvalidOperationException("Applicant information could not be retrieved.");
        }

        ApplicantInfoViewModel viewModel = new()
        {
            ApplicationId            = applicationId,
            ApplicationFormId        = applicantInfoDto.ApplicationFormId,
            ApplicationFormVersionId = applicationFormVersionId,
            ApplicantId              = applicantInfoDto.ApplicantId,
            ApplicantSummary         = ObjectMapper.Map<ApplicantSummaryDto, ApplicantSummaryViewModel>(applicantInfoDto.ApplicantSummary ?? new ApplicantSummaryDto()),
            ContactInfo              = ObjectMapper.Map<ContactInfoDto, ContactInfoViewModel>(applicantInfoDto.ContactInfo ?? new ContactInfoDto()),
            SigningAuthority         = ObjectMapper.Map<SigningAuthorityDto, SigningAuthorityViewModel>(applicantInfoDto.SigningAuthority ?? new SigningAuthorityDto()),
            ApplicantElectoralAddressType = electoralDistrictAddressType,
        };

        viewModel.ApplicantSummary.ApplicantId = applicantInfoDto.ApplicantId;

        await PopulateSectorsAndSubSectorsAsync(viewModel);
        await PopulateElectoralDistrictsAsync(viewModel);

        // MAPADDRESSES
        if (applicantInfoDto.ApplicantAddresses?.Count > 0)
        {
            // Map physical address
            var physicalAddress = FindMostRecentAddress(applicantInfoDto.ApplicantAddresses, AddressType.PhysicalAddress);
            if (physicalAddress is not null)
            {
                viewModel.PhysicalAddress = ObjectMapper.Map<ApplicantAddressDto, ApplicantAddressViewModel>(physicalAddress);
            }

            // Map mailing address
            var mailingAddress = FindMostRecentAddress(applicantInfoDto.ApplicantAddresses, AddressType.MailingAddress);
            if (mailingAddress is not null)
            {
                viewModel.MailingAddress = ObjectMapper.Map<ApplicantAddressDto, ApplicantAddressViewModel>(mailingAddress);
            }
        }

        return View(viewModel);
    }

    private static ApplicantAddressDto? FindMostRecentAddress(List<ApplicantAddressDto> applicantAddresses, AddressType addressType)
    {
        return applicantAddresses
                    .Where(address => address.AddressType == addressType)
                    .OrderByDescending(address =>
                            address.LastModificationTime.GetValueOrDefault(address.CreationTime))
                    .FirstOrDefault();
    }

    private async Task PopulateElectoralDistrictsAsync(ApplicantInfoViewModel model)
    {
        List<ElectoralDistrictDto> electoralDistricts = [.. (await applicationElectoralDistrictAppService.GetListAsync())];

        model.ElectoralDistrictList.AddRange(electoralDistricts.Select(electoralDistrict =>
            new SelectListItem
            {
                Value = electoralDistrict.ElectoralDistrictName,
                Text = electoralDistrict.ElectoralDistrictName
            }));
    }

    private async Task PopulateSectorsAndSubSectorsAsync(ApplicantInfoViewModel model)
    {
        List<SectorDto> sectors = [.. (await applicationSectorAppService.GetListAsync())];

        model.ApplicationSectors = sectors;

        model.ApplicationSectorsList.AddRange(sectors.Select(sector =>
            new SelectListItem
            {
                Value = sector.SectorName,
                Text = sector.SectorName
            }));

        if (sectors.Count > 0 && model.ApplicantSummary != null)
        {
            List<SubSectorDto> SubSectors = [];

            SectorDto? applicationSector = sectors.Find(x => x.SectorName == model.ApplicantSummary.Sector);
            SubSectors = applicationSector?.SubSectors ?? SubSectors;

            model.ApplicationSubSectorsList.AddRange(SubSectors.Select(SubSector =>
                new SelectListItem { Value = SubSector.SubSectorName, Text = SubSector.SubSectorName }));
        }
    }
}

public class ApplicantInfoStyleBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files
          .AddIfNotContains("/Views/Shared/Components/ApplicantInfo/Default.css");
    }
}

public class ApplicantInfoScriptBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files
          .AddIfNotContains("/Views/Shared/Components/ApplicantInfo/Default.js");
    }
}
