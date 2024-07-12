using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Unity.GrantManager.GrantApplications;
using System.Linq;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Unity.GrantManager.Locality;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo
{

    [Widget(
        RefreshUrl = "Widget/ApplicantInfo/Refresh",
        ScriptTypes = [typeof(ApplicantInfoScriptBundleContributor)],
        StyleTypes = [typeof(ApplicantInfoStyleBundleContributor)],
        AutoInitialize = true)]
    public class ApplicantInfoViewComponent : AbpViewComponent
    {
        private readonly IApplicationApplicantAppService _applicationAppicantService;
        private readonly ISectorService _applicationSectorAppService;

        public ApplicantInfoViewComponent(
            IApplicationApplicantAppService applicationAppicantService,
            ISectorService applicationSectorAppService)
        {
            _applicationAppicantService = applicationAppicantService;
            _applicationSectorAppService = applicationSectorAppService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId, Guid applicationFormVersionId)
        {
            var applicatInfoDto = await _applicationAppicantService.GetByApplicationIdAsync(applicationId);
            List<SectorDto> Sectors = [.. (await _applicationSectorAppService.GetListAsync())];

            ApplicantInfoViewModel model = new()
            {
                ApplicationId = applicationId,
                ApplicationFormId = applicatInfoDto.ApplicationFormId,
                ApplicationFormVersionId = applicationFormVersionId,
                ApplicationSectors = Sectors,
                ApplicantId = applicatInfoDto.ApplicantId
            };

            model.ApplicationSectorsList.AddRange(Sectors.Select(Sector =>
                new SelectListItem { Value = Sector.SectorName, Text = Sector.SectorName }));


            if (Sectors.Count > 0)
            {
                List<SubSectorDto> SubSectors = [];

                SectorDto? applicationSector = Sectors.Find(x => x.SectorName == applicatInfoDto.Sector);
                SubSectors = applicationSector?.SubSectors ?? SubSectors;

                model.ApplicationSubSectorsList.AddRange(SubSectors.Select(SubSector =>
                    new SelectListItem { Value = SubSector.SubSectorName, Text = SubSector.SubSectorName }));
            }


            model.ApplicantInfo = new()
            {
                Sector = applicatInfoDto.Sector,
                SubSector = applicatInfoDto.SubSector,
                ContactFullName = applicatInfoDto.ContactFullName,
                ContactTitle = applicatInfoDto.ContactTitle,
                ContactEmail = applicatInfoDto.ContactEmail,
                ContactBusinessPhone = applicatInfoDto.ContactBusinessPhone,
                ContactCellPhone = applicatInfoDto.ContactCellPhone,
                OrgName = applicatInfoDto.OrganizationName,
                OrgNumber = applicatInfoDto.OrgNumber,
                OrgStatus = applicatInfoDto.OrgStatus,
                OrganizationType = applicatInfoDto.OrganizationType,
                SigningAuthorityFullName = applicatInfoDto.SigningAuthorityFullName,
                SigningAuthorityTitle = applicatInfoDto.SigningAuthorityTitle,
                SigningAuthorityEmail = applicatInfoDto.SigningAuthorityEmail,
                SigningAuthorityBusinessPhone = applicatInfoDto.SigningAuthorityBusinessPhone,
                SigningAuthorityCellPhone = applicatInfoDto.SigningAuthorityCellPhone,
                OrganizationSize = applicatInfoDto.OrganizationSize,
                SectorSubSectorIndustryDesc = applicatInfoDto.SectorSubSectorIndustryDesc,
            };

            if (applicatInfoDto.ApplicantAddresses.Count != 0)
            {
                ApplicantAddressDto? physicalAddress = applicatInfoDto.ApplicantAddresses.Find(address => address.AddressType == AddressType.PhysicalAddress);

                if (physicalAddress != null)
                {
                    model.ApplicantInfo.PhysicalAddressStreet = physicalAddress.Street;
                    model.ApplicantInfo.PhysicalAddressStreet2 = physicalAddress.Street2;
                    model.ApplicantInfo.PhysicalAddressUnit = physicalAddress.Unit;
                    model.ApplicantInfo.PhysicalAddressCity = physicalAddress.City;
                    model.ApplicantInfo.PhysicalAddressProvince = physicalAddress.Province;
                    model.ApplicantInfo.PhysicalAddressPostalCode = physicalAddress.Postal;
                }

                ApplicantAddressDto? mailingAddress = applicatInfoDto.ApplicantAddresses.Find(address => address.AddressType == AddressType.MailingAddress);

                if (mailingAddress != null)
                {
                    model.ApplicantInfo.MailingAddressStreet = mailingAddress.Street;
                    model.ApplicantInfo.MailingAddressStreet2 = mailingAddress.Street2;
                    model.ApplicantInfo.MailingAddressUnit = mailingAddress.Unit;
                    model.ApplicantInfo.MailingAddressCity = mailingAddress.City;
                    model.ApplicantInfo.MailingAddressProvince = mailingAddress.Province;
                    model.ApplicantInfo.MailingAddressPostalCode = mailingAddress.Postal;
                }
            }

            return View(model);
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
}
