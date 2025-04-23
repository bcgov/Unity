using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Locality;
using Unity.Modules.Shared;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using static Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo.ApplicantInfoViewModel;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo;


[Widget(
    RefreshUrl = "Widget/ApplicantInfo/Refresh",
    ScriptTypes = [typeof(ApplicantInfoScriptBundleContributor)],
    StyleTypes = [typeof(ApplicantInfoStyleBundleContributor)],
    RequiredPolicies = new[] { UnitySelector.Applicant.Default },
    AutoInitialize = true)]
public class ApplicantInfoViewComponent(
    IApplicationApplicantAppService applicationAppicantService,
    ISectorService applicationSectorAppService) : AbpViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(Guid applicationId, Guid applicationFormVersionId)
    {
        var applicantInfoDto    = await applicationAppicantService.GetByApplicationIdAsync(applicationId);
        var newApplicantInfoDto = await applicationAppicantService.GetApplicantInfoTabAsync(applicationId);

        if (applicantInfoDto == null || newApplicantInfoDto == null)
        {
            throw new InvalidOperationException("Applicant information could not be retrieved.");
        }

        ApplicantInfoViewModel viewModel = new()
        {
            ApplicationId            = applicationId,
            ApplicationFormId        = newApplicantInfoDto.ApplicationFormId,
            ApplicationFormVersionId = applicationFormVersionId,
            ApplicantId              = newApplicantInfoDto.ApplicantId,
            ApplicantSummary         = ObjectMapper.Map<ApplicantSummaryDto, ApplicantSummaryViewModel>(newApplicantInfoDto.ApplicantSummary ?? new ApplicantSummaryDto()),
            ApplicantAddresses       = ObjectMapper.Map<List<ApplicantAddressDto>, List<ApplicantAddressViewModel>>(newApplicantInfoDto.ApplicantAddresses ?? []),
            ContactInfo              = ObjectMapper.Map<ContactInfoDto, ContactInfoViewModel>(newApplicantInfoDto.ContactInfo ?? new ContactInfoDto()),
            SigningAuthority         = ObjectMapper.Map<SigningAuthorityDto, SigningAuthorityViewModel>(newApplicantInfoDto.SigningAuthority ?? new SigningAuthorityDto())
        };

        // MAP SECTOR OPTIONS
        var sectors = await applicationSectorAppService.GetListAsync();
        viewModel.ApplicationSectors = sectors.ToList();
        viewModel.ApplicationSectorsList.AddRange(viewModel.ApplicationSectors.Select(sector =>
            new SelectListItem { Value = sector.SectorName, Text = sector.SectorName }));

        if (viewModel.ApplicationSectors.Count > 0 && newApplicantInfoDto.ApplicantSummary != null)
        {
            var applicationSector = viewModel.ApplicationSectors.FirstOrDefault(x => x.SectorName == newApplicantInfoDto.ApplicantSummary.Sector);
            var subSectors = applicationSector?.SubSectors ?? [];

            viewModel.ApplicationSubSectorsList.AddRange(subSectors.Select(subSector =>
                new SelectListItem { Value = subSector.SubSectorName, Text = subSector.SubSectorName }));
        }

        // MAP SPECIFIC ADDRESSES
        if (newApplicantInfoDto.ApplicantAddresses?.Count > 0)
        {
            var physicalAddress = newApplicantInfoDto.ApplicantAddresses
                .Where(address => address.AddressType == AddressType.PhysicalAddress)
                .OrderByDescending(address => address.CreationTime)
                .FirstOrDefault();

            MapAddress(physicalAddress, viewModel.PhysicalAddress);

            var mailingAddress = newApplicantInfoDto.ApplicantAddresses
                .Where(address => address.AddressType == AddressType.MailingAddress)
                .OrderByDescending(address => address.CreationTime)
                .FirstOrDefault();

            MapAddress(mailingAddress, viewModel.MailingAddress);
        }

        return View(viewModel);
    }

    private void MapAddress(ApplicantAddressDto? sourceAddress, ApplicantAddressViewModel targetAddress)
    {
        if (sourceAddress == null) return;

        targetAddress.Street = sourceAddress.Street;
        targetAddress.Street2 = sourceAddress.Street2;
        targetAddress.Unit = sourceAddress.Unit;
        targetAddress.City = sourceAddress.City;
        targetAddress.Province = sourceAddress.Province;
        targetAddress.PostalCode = sourceAddress.Postal;
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
        context.Files
          .AddIfNotContains("/libs/jquery-maskmoney/dist/jquery.maskMoney.min.js");
    }
}
