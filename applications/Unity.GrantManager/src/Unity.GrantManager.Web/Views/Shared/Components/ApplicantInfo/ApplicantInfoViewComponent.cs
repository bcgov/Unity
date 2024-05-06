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
        ScriptTypes = new[] { typeof(ApplicantInfoScriptBundleContributor) },
        StyleTypes = new[] { typeof(ApplicantInfoStyleBundleContributor) },
        AutoInitialize = true)]
    public class ApplicantInfoViewComponent : AbpViewComponent
    {
        private readonly IGrantApplicationAppService _grantApplicationAppService;
        private readonly ISectorService _applicationSectorAppService;

        public ApplicantInfoViewComponent(
            IGrantApplicationAppService grantApplicationAppService,
            ISectorService applicationSectorAppService
            )
        {
            _grantApplicationAppService = grantApplicationAppService;
            _applicationSectorAppService = applicationSectorAppService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {

            GrantApplicationDto application = await _grantApplicationAppService.GetAsync(applicationId);

            List<SectorDto> Sectors = (await _applicationSectorAppService.GetListAsync()).ToList();

            ApplicantInfoViewModel model = new()
            {
                ApplicationId = applicationId,
                ApplicationSectors = Sectors,
                ApplicantId = application.Applicant.Id
            };

            model.ApplicationSectorsList.AddRange(Sectors.Select(Sector =>
                new SelectListItem { Value = Sector.SectorName, Text = Sector.SectorName }));


            if (Sectors.Count > 0)
            {
                List<SubSectorDto> SubSectors = new();
                
                SectorDto? applicationSector = Sectors.Find(x => x.SectorName == application.Sector);
                SubSectors = applicationSector?.SubSectors ?? SubSectors;                

                model.ApplicationSubSectorsList.AddRange(SubSectors.Select(SubSector =>
                    new SelectListItem { Value = SubSector.SubSectorName, Text = SubSector.SubSectorName }));
            }


            model.ApplicantInfo = new()
            {

                Sector = application.Sector,
                SubSector = application.SubSector,
                ContactFullName = application.ContactFullName,
                ContactTitle = application.ContactTitle,
                ContactEmail = application.ContactEmail,
                ContactBusinessPhone = application.ContactBusinessPhone,
                ContactCellPhone = application.ContactCellPhone,
                OrgName = application.OrganizationName,
                OrgNumber = application.OrgNumber,
                OrgStatus = application.OrgStatus,
                OrganizationType = application.OrganizationType,
                SigningAuthorityFullName = application.SigningAuthorityFullName,
                SigningAuthorityTitle = application.SigningAuthorityTitle,
                SigningAuthorityEmail = application.SigningAuthorityEmail,
                SigningAuthorityBusinessPhone = application.SigningAuthorityBusinessPhone,
                SigningAuthorityCellPhone = application.SigningAuthorityCellPhone,
                OrganizationSize = application.OrganizationSize,
                SectorSubSectorIndustryDesc = application.SectorSubSectorIndustryDesc,
            };

            if (application.ApplicantAddresses.Any())
            {
                ApplicantAddressDto physicalAddress = application.ApplicantAddresses.First(address => address.AddressType == ApplicantAddressType.PHYSICAL_ADDRESS);
                model.ApplicantInfo.PhysicalAddressStreet = physicalAddress.Street;
                model.ApplicantInfo.PhysicalAddressUnit = physicalAddress.Unit;
                model.ApplicantInfo.PhysicalAddressCity = physicalAddress.City;
                model.ApplicantInfo.PhysicalAddressProvince = physicalAddress.Province;
                model.ApplicantInfo.PhysicalAddressPostalCode = physicalAddress.Postal;

                ApplicantAddressDto mailingAddress = application.ApplicantAddresses.First(address => address.AddressType == ApplicantAddressType.MAILING_ADDRESS);
                model.ApplicantInfo.MailingAddressStreet = mailingAddress.Street;
                model.ApplicantInfo.MailingAddressUnit = mailingAddress.Unit;
                model.ApplicantInfo.MailingAddressCity = mailingAddress.City;
                model.ApplicantInfo.MailingAddressProvince = mailingAddress.Province;
                model.ApplicantInfo.MailingAddressPostalCode = mailingAddress.Postal;
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
