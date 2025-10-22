using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using System.Collections.Generic;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantAddresses
{
    [Widget(
        RefreshUrl = "Widget/ApplicantAddresses/Refresh",
        ScriptTypes = new[] { typeof(ApplicantAddressesScriptBundleContributor) },
        StyleTypes = new[] { typeof(ApplicantAddressesStyleBundleContributor) },
        AutoInitialize = true)]
    public class ApplicantAddressesViewComponent : AbpViewComponent
    {
        private readonly IApplicantAddressRepository _applicantAddressRepository;

        public ApplicantAddressesViewComponent(IApplicantAddressRepository applicantAddressRepository)
        {
            _applicantAddressRepository = applicantAddressRepository;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicantId)
        {
            if (applicantId == Guid.Empty)
            {
                return View(new ApplicantAddressesViewModel { ApplicantId = applicantId });
            }

            try
            {
                // Load addresses using repository method
                // Note: The repository method returns addresses without Application navigation property loaded
                // We'll handle null Application gracefully in the mapping
                var addresses = await _applicantAddressRepository.FindByApplicantIdAsync(applicantId);

                var viewModel = new ApplicantAddressesViewModel
                {
                    ApplicantId = applicantId,
                    Addresses = addresses
                        .OrderByDescending(a => a.CreationTime)
                        .Select(a => new ApplicantAddressItemDto
                        {
                            Id = a.Id,
                            AddressType = GetAddressTypeName(a.AddressType),
                            ReferenceNo = a.ApplicationId.HasValue && a.Application != null
                                ? a.Application.ReferenceNo
                                : "N/A",
                            Street = a.Street ?? string.Empty,
                            Street2 = a.Street2 ?? string.Empty,
                            Unit = a.Unit ?? string.Empty,
                            City = a.City ?? string.Empty,
                            Province = a.Province ?? string.Empty,
                            Postal = a.Postal ?? string.Empty,
                            Country = a.Country ?? string.Empty
                        }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                return View(new ApplicantAddressesViewModel { ApplicantId = applicantId });
            }
        }

        private string GetAddressTypeName(GrantApplications.AddressType addressType)
        {
            return addressType switch
            {
                GrantApplications.AddressType.PhysicalAddress => "Physical Address",
                GrantApplications.AddressType.MailingAddress => "Mailing Address",
                GrantApplications.AddressType.BusinessAddress => "Business Address",
                _ => "Unknown"
            };
        }
    }

    public class ApplicantAddressesStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
                .AddIfNotContains("/Views/Shared/Components/ApplicantAddresses/Default.css");
        }
    }

    public class ApplicantAddressesScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
                .AddIfNotContains("/Views/Shared/Components/ApplicantAddresses/Default.js");
        }
    }
}
