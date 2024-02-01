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

namespace Unity.GrantManager.Web.Views.Shared.Components.ProjectInfo
{

    [Widget(
        RefreshUrl = "Widget/ProjectInfo/Refresh",
        ScriptTypes = new[] { typeof(ProjectInfoScriptBundleContributor) },
        StyleTypes = new[] { typeof(ProjectInfoStyleBundleContributor) },
        AutoInitialize = true)]
    public class ProjectInfoViewComponent : AbpViewComponent
    {
        private readonly IGrantApplicationAppService _grantApplicationAppService;
        private readonly ISectorService _applicationSectorAppService;
        private readonly IEconomicRegionService _applicationEconomicRegionAppService;
        private readonly IElectoralDistrictService _applicationElectoralDistrictAppService;
        private readonly IRegionalDistrictService _applicationRegionalDistrictAppService;
        private readonly ICensusSubdivisionService _applicationCensusSubdivisionAppService;

        public ProjectInfoViewComponent(
            IGrantApplicationAppService grantApplicationAppService,
            ISectorService applicationSectorAppService,
            IEconomicRegionService applicationEconomicRegionAppService,
            IElectoralDistrictService applicationElectoralDistrictAppService,
            IRegionalDistrictService applicationRegionalDistrictAppService,
            ICensusSubdivisionService applicationCensusSubdivisionAppService
            )
        {
            _grantApplicationAppService = grantApplicationAppService;
            _applicationSectorAppService = applicationSectorAppService;
            _applicationEconomicRegionAppService = applicationEconomicRegionAppService;
            _applicationElectoralDistrictAppService = applicationElectoralDistrictAppService;
            _applicationRegionalDistrictAppService = applicationRegionalDistrictAppService;
            _applicationCensusSubdivisionAppService = applicationCensusSubdivisionAppService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            const decimal ProjectFundingMax = 10000000;
            const decimal ProjectFundingMultiply = 0.2M;
            GrantApplicationDto application = await _grantApplicationAppService.GetAsync(applicationId);

            List<SectorDto> Sectors = (await _applicationSectorAppService.GetListAsync()).ToList();

            List<EconomicRegionDto> EconomicRegions = (await _applicationEconomicRegionAppService.GetListAsync()).ToList();

            List<ElectoralDistrictDto> ElectoralDistricts = (await _applicationElectoralDistrictAppService.GetListAsync()).ToList();

            List<RegionalDistrictDto> RegionalDistricts = (await _applicationRegionalDistrictAppService.GetListAsync()).ToList();

            List<CensusSubdivisionDto> CensusSubdivisions = (await _applicationCensusSubdivisionAppService.GetListAsync()).ToList();

            ProjectInfoViewModel model = new()
            {
                ApplicationId = applicationId,
                ApplicationSectors = Sectors,
                RegionalDistricts = RegionalDistricts,
                CensusSubdivisions = CensusSubdivisions,
                EconomicRegions = EconomicRegions,
            };

            model.ApplicationSectorsList.AddRange(Sectors.Select(Sector =>
                new SelectListItem { Value = Sector.SectorName, Text = Sector.SectorName }));
            
            model.EconomicRegionList.AddRange(EconomicRegions.Select(EconomicRegion =>  
                new SelectListItem { Value = EconomicRegion.EconomicRegionName, Text = EconomicRegion.EconomicRegionName }));

            model.ElectoralDistrictList.AddRange(ElectoralDistricts.Select(ElectoralDistrict =>
                new SelectListItem { Value = ElectoralDistrict.ElectoralDistrictName, Text = ElectoralDistrict.ElectoralDistrictName }));

            model.RegionalDistrictList.AddRange(RegionalDistricts.Select(RegionalDistrict => 
                new SelectListItem { Value = RegionalDistrict.RegionalDistrictName, Text = RegionalDistrict.RegionalDistrictName }));

            model.CensusSubdivisionList.AddRange(CensusSubdivisions.Select(CensusSubdivision =>
                new SelectListItem { Value = CensusSubdivision.CensusSubdivisionName, Text = CensusSubdivision.CensusSubdivisionName }));

            if (Sectors.Count > 0)
            {
                List<SubSectorDto> SubSectors = new();
                if (string.IsNullOrEmpty(application.SubSector))
                {
                    SubSectors = Sectors[0].SubSectors ?? SubSectors;
                }
                else
                {
                    SectorDto? applicationSector = Sectors.Find(x => x.SectorName == application.Sector);                                                                
                    SubSectors = applicationSector?.SubSectors ?? SubSectors;
                }

                model.ApplicationSubSectorsList.AddRange(SubSectors.Select(SubSector => 
                    new SelectListItem { Value = SubSector.SubSectorName, Text = SubSector.SubSectorName }));
            }


            decimal projectFundingTotal = application.ProjectFundingTotal ?? 0;
            double percentageTotalProjectBudget = application.PercentageTotalProjectBudget ?? 0;

            if (projectFundingTotal == 0)
            {
                projectFundingTotal = decimal.Multiply(application.TotalProjectBudget, ProjectFundingMultiply);
                projectFundingTotal = (projectFundingTotal > ProjectFundingMax) ? ProjectFundingMax : projectFundingTotal;
            }

            if (percentageTotalProjectBudget == 0)
            {
                percentageTotalProjectBudget = application.TotalProjectBudget == 0 ? 0 : decimal.Divide(application.RequestedAmount, application.TotalProjectBudget).To<double>();
            }

            model.IsFinalDecisionMade = GrantApplicationStateGroups.FinalDecisionStates.Contains(application.StatusCode);

            model.ProjectInfo = new()
            {
                ProjectName = application.ProjectName,
                ProjectSummary = application.ProjectSummary,
                ProjectStartDate = application.ProjectStartDate,
                ProjectEndDate = application.ProjectEndDate,
                RequestedAmount = application.RequestedAmount,
                TotalProjectBudget = application.TotalProjectBudget,
                ProjectFundingTotal = projectFundingTotal,
                PercentageTotalProjectBudget = Math.Round(percentageTotalProjectBudget, 2),
                Community = application.Community,
                CommunityPopulation = application.CommunityPopulation ?? 0,
                Forestry = application.Forestry,
                ForestryFocus = application.ForestryFocus,
                Acquisition = application.Acquisition,
                Sector = application.Sector,
                SubSector = application.SubSector,
                EconomicRegion = application.EconomicRegion,
                ElectoralDistrict = application.ElectoralDistrict,
                CensusSubdivision = application.CensusSubdivision,
                RegionalDistrict = application.RegionalDistrict,
                ContactFullName = application.ContactFullName,
                ContactTitle = application.ContactTitle,
                ContactEmail = application.ContactEmail,
                ContactBusinessPhone = application.ContactBusinessPhone,
                ContactCellPhone = application.ContactCellPhone
            };

            return View(model);
        }
    }

    public class ProjectInfoStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ProjectInfo/Default.css");
        }
    }

    public class ProjectInfoScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ProjectInfo/Default.js");
            context.Files
              .AddIfNotContains("/libs/jquery-maskmoney/dist/jquery.maskMoney.min.js");
        }
    }
}
