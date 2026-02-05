using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Locality;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantOrganizationInfo
{
    [Widget(
        RefreshUrl = "Widget/ApplicantOrganizationInfo/Refresh",
        ScriptTypes = new[] { typeof(ApplicantOrganizationInfoScriptBundleContributor) },
        StyleTypes = new[] { typeof(ApplicantOrganizationInfoStyleBundleContributor) },
        AutoInitialize = true)]
    public class ApplicantOrganizationInfoViewComponent : AbpViewComponent
    {
        private readonly IApplicantRepository _applicantRepository;
        private readonly ISectorService _sectorService;        

        public ApplicantOrganizationInfoViewComponent(
            IApplicantRepository applicantRepository,
            ISectorService sectorService)
        {
            _applicantRepository = applicantRepository;
            _sectorService = sectorService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicantId)
        {
            if (applicantId == Guid.Empty)
            {
                return View(new ApplicantOrganizationInfoViewModel());
            }

            try
            {
                var applicant = await _applicantRepository.GetAsync(applicantId);

                var viewModel = new ApplicantOrganizationInfoViewModel
                {
                    ApplicantId = applicantId,

                    // Organization Summary
                    UnityApplicantId = applicant.UnityApplicantId ?? string.Empty,
                    ApplicantName = applicant.ApplicantName ?? string.Empty,
                    OrgName = applicant.OrgName ?? string.Empty,
                    OrgNumber = applicant.OrgNumber ?? string.Empty,
                    BusinessNumber = applicant.BusinessNumber ?? string.Empty,
                    OrgStatus = applicant.OrgStatus ?? string.Empty,
                    OrganizationType = applicant.OrganizationType ?? string.Empty,
                    OrganizationSize = applicant.OrganizationSize ?? string.Empty,                    
                    NonRegOrgName = applicant.NonRegOrgName ?? string.Empty,
                    
                    // Sector Information
                    Sector = applicant.Sector ?? string.Empty,
                    SubSector = applicant.SubSector ?? string.Empty,
                    IndigenousOrgInd = applicant.IndigenousOrgInd == "true" || applicant.IndigenousOrgInd == "True" || applicant.IndigenousOrgInd == "Yes",
                    SectorSubSectorIndustryDesc = applicant.SectorSubSectorIndustryDesc ?? string.Empty,

                    // Financial Information
                    FiscalMonth = applicant.FiscalMonth ?? string.Empty,
                    FiscalDay = applicant.FiscalDay?.ToString() ?? string.Empty,
                    OrganizationOperationLength = applicant.OrganizationOperationLength ?? string.Empty,
                    RedStop = applicant.RedStop == true
                };

                await PopulateOptionListsAsync(viewModel);

                return View(viewModel);
            }
            catch (Exception)
            {
                return View(new ApplicantOrganizationInfoViewModel());
            }
        }

        private async Task PopulateOptionListsAsync(ApplicantOrganizationInfoViewModel model)
        {
            model.OrgStatusList = FormatOptionsList(ProjectInfoOptionsList.OrgBookStatusList);
            model.OrganizationTypeList = FormatOptionsList(ProjectInfoOptionsList.OrganizationTypeList);
            model.FiscalDayList = [.. FormatOptionsList(ApplicantInfoOptionsList.FiscalDayList)
                .OrderBy(item => int.Parse(item.Text, CultureInfo.InvariantCulture))];
            model.FiscalMonthList = [.. FormatOptionsList(ApplicantInfoOptionsList.FiscalMonthList)
                .OrderBy(item => DateTime.ParseExact(item.Text, "MMMM", CultureInfo.InvariantCulture).Month)];
            model.OrganizationOperationLengthList = FormatOptionsList(ApplicantInfoOptionsList.OrganizationOperationLengthList);

            AddDefaultOption(model.OrgStatusList);
            AddDefaultOption(model.OrganizationTypeList);
            AddDefaultOption(model.FiscalDayList);
            AddDefaultOption(model.FiscalMonthList);
            AddDefaultOption(model.OrganizationOperationLengthList);

            IList<SectorDto> sectors = await _sectorService.GetListAsync();
            model.Sectors = [.. sectors];

            model.SectorList = [.. sectors.Select(sector => new SelectListItem { Value = sector.SectorName, Text = sector.SectorName })];
            AddDefaultOption(model.SectorList);

            model.SubSectorList = BuildSubSectorList(model.Sectors, model.Sector);
        }

        private static List<SelectListItem> FormatOptionsList(ImmutableDictionary<string, string> source)
        {
            List<SelectListItem> items = [];

            foreach (KeyValuePair<string, string> entry in source)
            {
                items.Add(new SelectListItem { Value = entry.Key, Text = entry.Value });
            }

            return items;
        }

        private static List<SelectListItem> BuildSubSectorList(IEnumerable<SectorDto> sectors, string? selectedSector)
        {
            IEnumerable<SubSectorDto> subSectors = sectors
                .FirstOrDefault(sector => sector.SectorName == selectedSector)?.SubSectors ?? Enumerable.Empty<SubSectorDto>();

            List<SelectListItem> subSectorList = [.. subSectors.Select(subSector => new SelectListItem { Value = subSector.SubSectorName, Text = subSector.SubSectorName })];

            AddDefaultOption(subSectorList);

            return subSectorList;
        }

        private static void AddDefaultOption(List<SelectListItem> items)
        {
            if (items.Exists(option => string.IsNullOrWhiteSpace(option.Value)))
            {
                return;
            }

            items.Insert(0, new SelectListItem { Value = string.Empty, Text = "Please choose..." });
        }
    }

    public class ApplicantOrganizationInfoStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
                .AddIfNotContains("/Views/Shared/Components/ApplicantOrganizationInfo/Default.css");
        }
    }

    public class ApplicantOrganizationInfoScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files.AddIfNotContains("/libs/jquery-maskmoney/dist/jquery.maskMoney.min.js");
            context.Files.AddIfNotContains("/Views/Shared/Components/ApplicantOrganizationInfo/Default.js");
        }
    }
}
