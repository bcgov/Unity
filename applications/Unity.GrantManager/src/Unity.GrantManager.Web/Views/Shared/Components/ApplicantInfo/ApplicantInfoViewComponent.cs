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
            var applicantInfoDto = await _applicationAppicantService.GetByApplicationIdAsync(applicationId);
            List<SectorDto> Sectors = [.. (await _applicationSectorAppService.GetListAsync())];

            ApplicantInfoViewModel model = new()
            {
                ApplicationId = applicationId,
                ApplicationFormId = applicantInfoDto.ApplicationFormId,
                ApplicationFormVersionId = applicationFormVersionId,
                ApplicationSectors = Sectors,
                ApplicantId = applicantInfoDto.ApplicantId
            };

            model.ApplicationSectorsList.AddRange(Sectors.Select(Sector =>
                new SelectListItem { Value = Sector.SectorName, Text = Sector.SectorName }));


            if (Sectors.Count > 0)
            {
                List<SubSectorDto> SubSectors = [];

                SectorDto? applicationSector = Sectors.Find(x => x.SectorName == applicantInfoDto.Sector);
                SubSectors = applicationSector?.SubSectors ?? SubSectors;

                model.ApplicationSubSectorsList.AddRange(SubSectors.Select(SubSector =>
                    new SelectListItem { Value = SubSector.SubSectorName, Text = SubSector.SubSectorName }));
            }


            model.ApplicantInfo = new()
            {
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
                NonRegOrgName = applicantInfoDto.NonRegOrgName
            };

            if (applicantInfoDto.ApplicantAddresses.Count != 0)
            {
                ApplicantAddressDto? physicalAddress = applicantInfoDto.ApplicantAddresses
                    .Where(address => address.AddressType == AddressType.PhysicalAddress)
                    .OrderByDescending(address => address.CreationTime)
                    .FirstOrDefault();

                if (physicalAddress != null)
                {
                    model.ApplicantInfo.PhysicalAddressStreet = physicalAddress.Street;
                    model.ApplicantInfo.PhysicalAddressStreet2 = physicalAddress.Street2;
                    model.ApplicantInfo.PhysicalAddressUnit = physicalAddress.Unit;
                    model.ApplicantInfo.PhysicalAddressCity = physicalAddress.City;
                    model.ApplicantInfo.PhysicalAddressProvince = physicalAddress.Province;
                    model.ApplicantInfo.PhysicalAddressPostalCode = physicalAddress.Postal;
                }
                
                ApplicantAddressDto? mailingAddress = applicantInfoDto.ApplicantAddresses
                    .Where(address => address.AddressType == AddressType.MailingAddress)
                    .OrderByDescending(address => address.CreationTime)
                    .FirstOrDefault();

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
