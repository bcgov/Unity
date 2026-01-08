using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.Modules.Shared;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.Authorization.Permissions;


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
        private readonly IApplicantAgentRepository _applicantAgentRepository;
        private readonly IPermissionChecker _permissionChecker;

        public ApplicantAddressesViewComponent(
            IApplicantAddressRepository applicantAddressRepository,
            IApplicantAgentRepository applicantAgentRepository,
            IPermissionChecker permissionChecker)
        {
            _applicantAddressRepository = applicantAddressRepository;
            _applicantAgentRepository = applicantAgentRepository;
            _permissionChecker = permissionChecker;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicantId)
        {
            if (applicantId == Guid.Empty)
            {
                return View(new ApplicantAddressesViewModel { ApplicantId = applicantId });
            }

            
            // Load addresses using repository method
            // Note: The repository method returns addresses without Application navigation property loaded
            // We'll handle null Application gracefully in the mapping
            var addresses = await _applicantAddressRepository.FindByApplicantIdAsync(applicantId);
            var agents = await _applicantAgentRepository.GetListByApplicantIdAsync(applicantId);

            var orderedAddresses = addresses
                .OrderByDescending(a => a.LastModificationTime ?? a.CreationTime)
                .ToList();

            var orderedAgents = agents
                .OrderByDescending(a => a.LastModificationTime ?? a.CreationTime)
                .ToList();

            var viewModel = new ApplicantAddressesViewModel
            {
                ApplicantId = applicantId,
                CanEditContact = await _permissionChecker.IsGrantedAsync(UnitySelector.Applicant.Contact.Update),
                CanEditAddress = await _permissionChecker.IsGrantedAsync(UnitySelector.Applicant.Location.Update),
                Addresses = orderedAddresses
                    .Select(a => new ApplicantAddressItemDto
                    {
                        Id = a.Id,
                        AddressType = GetAddressTypeName(a.AddressType),                        
                        Street = a.Street ?? string.Empty,
                        Street2 = a.Street2 ?? string.Empty,
                        Unit = a.Unit ?? string.Empty,
                        City = a.City ?? string.Empty,
                        Province = a.Province ?? string.Empty,
                        Postal = a.Postal ?? string.Empty,
                        Country = a.Country ?? string.Empty
                    }).ToList(),
                Contacts = orderedAgents
                    .Select((agent, index) => new ApplicantContactItemDto
                    {
                        Id = agent.Id,
                        Name = agent.Name ?? string.Empty,
                        Email = agent.Email ?? string.Empty,
                        Phone = !string.IsNullOrWhiteSpace(agent.Phone)
                            ? agent.Phone!
                            : agent.Phone2 ?? string.Empty,
                        Title = agent.Title ?? string.Empty,
                        Type = index == 0 ? "Primary" : "",
                        CreationTime = agent.CreationTime
                    })
                    .ToList()
            };

            var primaryContact = orderedAgents.FirstOrDefault();
            if (primaryContact != null)
            {
                viewModel.PrimaryContact = new ApplicantPrimaryContactViewModel
                {
                    Id = primaryContact.Id,
                    FullName = primaryContact.Name ?? string.Empty,
                    Title = primaryContact.Title ?? string.Empty,
                    Email = primaryContact.Email ?? string.Empty,
                    BusinessPhone = primaryContact.Phone ?? string.Empty,
                    CellPhone = primaryContact.Phone2 ?? string.Empty
                };
            }

            var primaryPhysicalAddress = FindMostRecentAddress(orderedAddresses, GrantApplications.AddressType.PhysicalAddress);
            if (primaryPhysicalAddress != null)
            {
                viewModel.PrimaryPhysicalAddress = MapPrimaryAddress(primaryPhysicalAddress);
            }

            var primaryMailingAddress = FindMostRecentAddress(orderedAddresses, GrantApplications.AddressType.MailingAddress);
            if (primaryMailingAddress != null)
            {
                viewModel.PrimaryMailingAddress = MapPrimaryAddress(primaryMailingAddress);
            }

            return View(viewModel);
            
        }

        private static ApplicantAddress? FindMostRecentAddress(IEnumerable<ApplicantAddress> addresses, GrantApplications.AddressType addressType)
        {
            return addresses
                .Where(address => address.AddressType == addressType)
                .OrderByDescending(address => address.LastModificationTime ?? address.CreationTime)
                .FirstOrDefault();
        }

        private static ApplicantPrimaryAddressViewModel MapPrimaryAddress(ApplicantAddress address)
        {
            return new ApplicantPrimaryAddressViewModel
            {
                Id = address.Id,
                Street = address.Street ?? string.Empty,
                Street2 = address.Street2 ?? string.Empty,
                Unit = address.Unit ?? string.Empty,
                City = address.City ?? string.Empty,
                Province = address.Province ?? string.Empty,
                PostalCode = address.Postal ?? string.Empty
            };
        }

        private static string GetAddressTypeName(GrantApplications.AddressType addressType)
        {
            return addressType switch
            {
                GrantApplications.AddressType.PhysicalAddress => "Physical",
                GrantApplications.AddressType.MailingAddress => "Mailing",
                GrantApplications.AddressType.BusinessAddress => "Business",
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
