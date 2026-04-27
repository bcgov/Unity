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
        private readonly IApplicantAgentRepository _applicantAgentRepository;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IRepository<Application, Guid> _applicationRepository;

        public ApplicantContactsViewComponent(
            IApplicantAgentRepository applicantAgentRepository,
            IPermissionChecker permissionChecker,
            IRepository<Application, Guid> applicationRepository)
        {
            _applicantAgentRepository = applicantAgentRepository;
            _permissionChecker = permissionChecker;
            _applicationRepository = applicationRepository;
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

            var appIds = new HashSet<Guid>(
                orderedAgents.Where(a => a.ApplicationId.HasValue).Select(a => a.ApplicationId!.Value));

            var appRefMap = new Dictionary<Guid, string>();
            if (appIds.Count > 0)
            {
                var apps = await _applicationRepository.GetListAsync(a => appIds.Contains(a.Id));
                foreach (var app in apps)
                {
                    appRefMap[app.Id] = app.ReferenceNo;
                }
            }

            var viewModel = new ApplicantContactsViewModel
            {
                ApplicantId = applicantId,
                CanEditContact = await _permissionChecker.IsGrantedAsync(UnitySelector.ApplicantManagement.Contacts.EditContacts),
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
                        CreationTime = agent.CreationTime,
                        ApplicationId = agent.ApplicationId,
                        ReferenceNo = agent.ApplicationId.HasValue ? appRefMap.GetValueOrDefault(agent.ApplicationId.Value, string.Empty) : string.Empty
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

            return View(viewModel);
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
