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
    public class ApplicantInfoViewComponent(
        IApplicationApplicantAppService applicationAppicantService,
        ISectorService applicationSectorAppService,
        IElectoralDistrictService applicationElectoralDistrictAppService) : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId, Guid applicationFormVersionId)
        {
            var applicantInfoDto = await applicationAppicantService.GetByApplicationIdAsync(applicationId);

            ApplicantInfoViewModel model = new()
            {
                ApplicationId = applicationId,
                ApplicationFormId = applicantInfoDto.ApplicationFormId,
                ApplicationFormVersionId = applicationFormVersionId,
                ApplicantId = applicantInfoDto.ApplicantId
            };

            await PopulateSectorsAndSubSectorsAsync(applicantInfoDto, model);
            await PopulateElectoralDistrictsAsync(model);

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
                NonRegOrgName = applicantInfoDto.NonRegOrgName,
                ElectoralDistrict = applicantInfoDto.ElectoralDistrict
            };

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

        private async Task PopulateSectorsAndSubSectorsAsync(ApplicationApplicantInfoDto applicantInfoDto, ApplicantInfoViewModel model)
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

                SectorDto? applicationSector = sectors.Find(x => x.SectorName == applicantInfoDto.Sector);
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
            context.Files
              .AddIfNotContains("/libs/jquery-maskmoney/dist/jquery.maskMoney.min.js");
        }
    }
}
