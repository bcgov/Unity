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
            ISectorService applicationSectorAppService
            )
        {
            _applicationAppicantService = applicationAppicantService;
            _applicationSectorAppService = applicationSectorAppService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            var applicatInfoDto = await _applicationAppicantService.GetByApplicationIdAsync(applicationId);
            List<SectorDto> Sectors = [.. (await _applicationSectorAppService.GetListAsync())];

            ApplicantInfoViewModel model = new()
            {
                ApplicationId = applicationId,
                ApplicationSectors = Sectors,
                ApplicantId = applicatInfoDto.ApplicantId
            };

            model.ApplicationSectorsList.AddRange(Sectors.Select(Sector =>
                new SelectListItem { Value = Sector.SectorName, Text = Sector.SectorName }));


            if (Sectors.Count > 0)
            {
                List<SubSectorDto> SubSectors = new();
                
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
