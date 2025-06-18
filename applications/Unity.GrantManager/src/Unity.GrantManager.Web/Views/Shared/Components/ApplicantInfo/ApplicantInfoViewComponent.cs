using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
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
        var applicantInfoDto = await applicationAppicantService.GetByApplicationIdAsync(applicationId);
        var applicationForm = await applicationFormAppService.GetAsync(applicantInfoDto.ApplicationFormId);

        ApplicantInfoViewModel model = new()
        {
            ApplicationId = applicationId,
            ApplicationFormId = applicantInfoDto.ApplicationFormId,
            ApplicationFormVersionId = applicationFormVersionId,
            ApplicantId = applicantInfoDto.ApplicantId,
            Sector = applicantInfoDto.Sector,
            SubSector = applicantInfoDto.SubSector,
            ContactFullName = applicantInfoDto.ContactFullName,
            ContactTitle = applicantInfoDto.ContactTitle,
            ContactEmail = applicantInfoDto.ContactEmail,
            ContactBusinessPhone = applicantInfoDto.ContactBusinessPhone,
            ContactCellPhone = applicantInfoDto.ContactCellPhone,
            OrgName = applicantInfoDto.OrganizationName,
            OrgNumber = applicantInfoDto.OrgNumber,
            OrgStatus = applicantInfoDto.OrgStatus,
            OrganizationType = applicantInfoDto.OrganizationType,
            SigningAuthorityFullName = applicantInfoDto.SigningAuthorityFullName,
            SigningAuthorityTitle = applicantInfoDto.SigningAuthorityTitle,
            SigningAuthorityEmail = applicantInfoDto.SigningAuthorityEmail,
            SigningAuthorityBusinessPhone = applicantInfoDto.SigningAuthorityBusinessPhone,
            SigningAuthorityCellPhone = applicantInfoDto.SigningAuthorityCellPhone,
            OrganizationSize = applicantInfoDto.OrganizationSize,
            SectorSubSectorIndustryDesc = applicantInfoDto.SectorSubSectorIndustryDesc,
            RedStop = applicantInfoDto.RedStop,
            IndigenousOrgInd = applicantInfoDto.IndigenousOrgInd,
            UnityApplicantId = applicantInfoDto.UnityApplicantId,
            FiscalDay = applicantInfoDto.FiscalDay,
            FiscalMonth = applicantInfoDto.FiscalMonth,
            NonRegOrgName = applicantInfoDto.NonRegOrgName,
            ElectoralDistrict = applicantInfoDto.ElectoralDistrict,
            ApplicantElectoralAddressType = applicationForm.ElectoralDistrictAddressType
                    ?? ApplicationForm.GetDefaultElectoralDistrictAddressType(),
        };

        await PopulateSectorsAndSubSectorsAsync(model);
        await PopulateElectoralDistrictsAsync(model);

        if (applicantInfoDto.ApplicantAddresses.Count != 0)
        {
            PopulateAddressInfo(applicantInfoDto, model);
        }

        return View(model);
    }

    private static void PopulateAddressInfo(ApplicationApplicantInfoDto applicantInfoDto, ApplicantInfoViewModel model)
    {
        ApplicantAddressDto? physicalAddress = applicantInfoDto.ApplicantAddresses
                .Where(address => address.AddressType == AddressType.PhysicalAddress)
                .OrderByDescending(address => address.CreationTime)
                .FirstOrDefault();

        if (physicalAddress != null)
        {
            model.PhysicalAddressStreet = physicalAddress.Street;
            model.PhysicalAddressStreet2 = physicalAddress.Street2;
            model.PhysicalAddressUnit = physicalAddress.Unit;
            model.PhysicalAddressCity = physicalAddress.City;
            model.PhysicalAddressProvince = physicalAddress.Province;
            model.PhysicalAddressPostalCode = physicalAddress.Postal;
        }

        ApplicantAddressDto? mailingAddress = applicantInfoDto.ApplicantAddresses
                .Where(address => address.AddressType == AddressType.MailingAddress)
                .OrderByDescending(address => address.CreationTime)
                .FirstOrDefault();

        if (mailingAddress != null)
        {
            model.MailingAddressStreet = mailingAddress.Street;
            model.MailingAddressStreet2 = mailingAddress.Street2;
            model.MailingAddressUnit = mailingAddress.Unit;
            model.MailingAddressCity = mailingAddress.City;
            model.MailingAddressProvince = mailingAddress.Province;
            model.MailingAddressPostalCode = mailingAddress.Postal;
        }
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

        if (sectors.Count > 0)
        {
            List<SubSectorDto> SubSectors = [];

            SectorDto? applicationSector = sectors.Find(x => x.SectorName == model.Sector);
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
          .AddIfNotContains("/Views/Shared/Components/_Shared/unity-form-component.js");
        context.Files
          .AddIfNotContains("/Views/Shared/Components/ApplicantInfo/Default.js");
        context.Files
          .AddIfNotContains("/libs/jquery-maskmoney/dist/jquery.maskMoney.min.js");
    }
}
