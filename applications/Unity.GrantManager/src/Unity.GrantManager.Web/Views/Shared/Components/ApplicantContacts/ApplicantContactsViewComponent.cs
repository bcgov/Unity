using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Contacts;
using Unity.Modules.Shared;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantContacts
{
    [Widget(
        RefreshUrl = "Widget/ApplicantContacts/Refresh",
        ScriptTypes = new[] { typeof(ApplicantContactsScriptBundleContributor) },
        StyleTypes = new[] { typeof(ApplicantContactsStyleBundleContributor) },
        AutoInitialize = true)]
    public class ApplicantContactsViewComponent : AbpViewComponent
    {
        private const string ApplicantEntityType = "Applicant";

        private readonly IApplicantAgentRepository _applicantAgentRepository;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IRepository<Application, Guid> _applicationRepository;
        private readonly IContactRepository _contactRepository;
        private readonly IContactLinkRepository _contactLinkRepository;

        public ApplicantContactsViewComponent(
            IApplicantAgentRepository applicantAgentRepository,
            IPermissionChecker permissionChecker,
            IRepository<Application, Guid> applicationRepository,
            IContactRepository contactRepository,
            IContactLinkRepository contactLinkRepository)
        {
            _applicantAgentRepository = applicantAgentRepository;
            _permissionChecker = permissionChecker;
            _applicationRepository = applicationRepository;
            _contactRepository = contactRepository;
            _contactLinkRepository = contactLinkRepository;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicantId)
        {
            if (applicantId == Guid.Empty)
            {
                return View(new ApplicantContactsViewModel { ApplicantId = applicantId });
            }

            var agents = await _applicantAgentRepository.GetListByApplicantIdAsync(applicantId);
            var orderedAgents = agents
                .OrderByDescending(a => a.LastModificationTime ?? a.CreationTime)
                .ToList();

            var appRefMap = await BuildApplicationReferenceMapAsync(orderedAgents);
            var linkedContacts = await GetLinkedContactsAsync(applicantId);
            var agentContacts = MapAgentContacts(orderedAgents, appRefMap);

            var allContacts = agentContacts.Concat(linkedContacts)
                .OrderByDescending(c => c.CreationTime)
                .ToList();

            ResolvePrimaryContact(allContacts);

            var viewModel = new ApplicantContactsViewModel
            {
                ApplicantId = applicantId,
                CanEditContact = await _permissionChecker.IsGrantedAsync(UnitySelector.Applicant.Contact.Update),
                Contacts = allContacts
            };

            var primaryContact = allContacts.FirstOrDefault(c => c.IsPrimary);
            if (primaryContact != null)
            {
                viewModel.PrimaryContact = new ApplicantPrimaryContactViewModel
                {
                    Id = primaryContact.Id,
                    Source = primaryContact.Source,
                    FullName = primaryContact.Name,
                    Title = primaryContact.Title,
                    Email = primaryContact.Email,
                    BusinessPhone = primaryContact.Phone,
                    CellPhone = string.Empty
                };
            }

            return View(viewModel);
        }

        private async Task<Dictionary<Guid, string>> BuildApplicationReferenceMapAsync(List<ApplicantAgent> agents)
        {
            var appIds = new HashSet<Guid>(
                agents.Where(a => a.ApplicationId.HasValue).Select(a => a.ApplicationId!.Value));

            var appRefMap = new Dictionary<Guid, string>();
            if (appIds.Count > 0)
            {
                var apps = await _applicationRepository.GetListAsync(a => appIds.Contains(a.Id));
                foreach (var app in apps)
                {
                    appRefMap[app.Id] = app.ReferenceNo;
                }
            }

            return appRefMap;
        }

        private static List<ApplicantContactItemDto> MapAgentContacts(
            List<ApplicantAgent> agents,
            Dictionary<Guid, string> appRefMap)
        {
            return agents
                .Select(agent => new ApplicantContactItemDto
                {
                    Id = agent.Id,
                    Name = agent.Name ?? string.Empty,
                    Email = agent.Email ?? string.Empty,
                    Phone = !string.IsNullOrWhiteSpace(agent.Phone)
                        ? agent.Phone!
                        : agent.Phone2 ?? string.Empty,
                    Title = agent.Title ?? string.Empty,
                    Type = string.Empty,
                    Source = "Agent",
                    IsPrimary = false,
                    CreationTime = agent.CreationTime,
                    ApplicationId = agent.ApplicationId,
                    ReferenceNo = agent.ApplicationId.HasValue
                        ? appRefMap.GetValueOrDefault(agent.ApplicationId.Value, string.Empty)
                        : string.Empty
                })
                .ToList();
        }

        private static void ResolvePrimaryContact(List<ApplicantContactItemDto> contacts)
        {
            var primaryContact = contacts.FirstOrDefault(c => c.IsPrimary)
                ?? contacts.FirstOrDefault();

            if (primaryContact != null)
            {
                primaryContact.IsPrimary = true;
            }
        }

        private async Task<List<ApplicantContactItemDto>> GetLinkedContactsAsync(Guid applicantId)
        {
            var contactLinksQuery = await _contactLinkRepository.GetQueryableAsync();
            var contactsQuery = await _contactRepository.GetQueryableAsync();

            return await (
                from link in contactLinksQuery
                join contact in contactsQuery on link.ContactId equals contact.Id
                where link.RelatedEntityType == ApplicantEntityType
                      && link.RelatedEntityId == applicantId
                      && link.IsActive
                select new ApplicantContactItemDto
                {
                    Id = contact.Id,
                    Name = contact.Name,
                    Email = contact.Email ?? string.Empty,
                    Phone = !string.IsNullOrWhiteSpace(contact.WorkPhoneNumber)
                        ? contact.WorkPhoneNumber!
                        : contact.MobilePhoneNumber ?? string.Empty,
                    Title = contact.Title ?? string.Empty,
                    Type = link.Role ?? string.Empty,
                    Source = "Contact",
                    IsPrimary = link.IsPrimary,
                    CreationTime = contact.CreationTime,
                    ApplicationId = null,
                    ReferenceNo = string.Empty
                }).ToListAsync();
        }
    }

    public class ApplicantContactsStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
                .AddIfNotContains("/Views/Shared/Components/ApplicantContacts/Default.css");
        }
    }

    public class ApplicantContactsScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
                .AddIfNotContains("/Views/Shared/Components/ApplicantContacts/Default.js");
        }
    }
}
