using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using System.Collections.Generic;


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

        public ApplicantOrganizationInfoViewComponent(IApplicantRepository applicantRepository)
        {
            _applicantRepository = applicantRepository;
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
                    OrgNumber = applicant.OrgNumber ?? string.Empty,
                    BusinessNumber = applicant.BusinessNumber ?? string.Empty,
                    OrgStatus = applicant.OrgStatus ?? string.Empty,
                    OrganizationType = applicant.OrganizationType ?? string.Empty,
                    OrganizationSize = applicant.OrganizationSize ?? string.Empty,
                    Status = applicant.Status ?? "Active",
                    NonRegisteredBusinessName = applicant.NonRegisteredBusinessName ?? string.Empty,
                    NonRegOrgName = applicant.NonRegOrgName ?? string.Empty,
                    ApproxNumberOfEmployees = applicant.ApproxNumberOfEmployees ?? string.Empty,

                    // Sector Information
                    Sector = applicant.Sector ?? string.Empty,
                    SubSector = applicant.SubSector ?? string.Empty,
                    IndigenousOrgInd = applicant.IndigenousOrgInd == "true" || applicant.IndigenousOrgInd == "True",
                    SectorSubSectorIndustryDesc = applicant.SectorSubSectorIndustryDesc ?? string.Empty,

                    // Financial Information
                    FiscalMonth = applicant.FiscalMonth ?? string.Empty,
                    FiscalDay = applicant.FiscalDay?.ToString() ?? string.Empty,
                    StartedOperatingDate = applicant.StartedOperatingDate?.ToString("yyyy-MM-dd") ?? string.Empty,

                    // Payment Information
                    SupplierId = applicant.SupplierId?.ToString() ?? string.Empty,
                    SiteId = applicant.SiteId?.ToString() ?? string.Empty,
                    ElectoralDistrict = applicant.ElectoralDistrict ?? string.Empty,

                    // Status Information
                    MatchPercentage = applicant.MatchPercentage.HasValue
                        ? $"{applicant.MatchPercentage.Value:F1}%"
                        : string.Empty,
                    IsDuplicated = applicant.IsDuplicated == true,
                    RedStop = applicant.RedStop == true
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                return View(new ApplicantOrganizationInfoViewModel());
            }
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
            context.Files
                .AddIfNotContains("/Views/Shared/Components/ApplicantOrganizationInfo/Default.js");
        }
    }
}
