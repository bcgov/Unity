using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.ObjectMapping;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantSubmissions
{
    [Widget(
        RefreshUrl = "Widget/ApplicantSubmissions/Refresh",
        ScriptTypes = new[] { typeof(ApplicantSubmissionsScriptBundleContributor) },
        StyleTypes = new[] { typeof(ApplicantSubmissionsStyleBundleContributor) },
        AutoInitialize = true)]
    public class ApplicantSubmissionsViewComponent : AbpViewComponent
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IObjectMapper _objectMapper;

        public ApplicantSubmissionsViewComponent(
            IApplicationRepository applicationRepository,
            IObjectMapper objectMapper)
        {
            _applicationRepository = applicationRepository;
            _objectMapper = objectMapper;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicantId)
        {
            // Handle empty GUID
            if (applicantId == Guid.Empty)
            {
                return View(new ApplicantSubmissionsViewModel
                {
                    ApplicantId = applicantId,
                    Submissions = new System.Collections.Generic.List<GrantApplicationDto>()
                });
            }

            // Query applications
            var applications = await _applicationRepository.GetByApplicantIdAsync(applicantId);

            // Map to DTOs (similar to GrantApplicationAppService.GetListAsync)
            var submissionDtos = applications.Select(app =>
            {
                var dto = _objectMapper.Map<Application, GrantApplicationDto>(app);

                // Map related entities
                dto.Status = app.ApplicationStatus?.InternalStatus ?? string.Empty;
                dto.Category = app.ApplicationForm?.Category ?? string.Empty;
                dto.Applicant = app.Applicant != null
                    ? _objectMapper.Map<Applicant, GrantApplicationApplicantDto>(app.Applicant)
                    : new GrantApplicationApplicantDto();
                dto.OrganizationName = app.Applicant?.OrgName ?? string.Empty;
                dto.NonRegOrgName = app.Applicant?.NonRegOrgName ?? string.Empty;
                dto.OrganizationType = app.Applicant?.OrganizationType ?? string.Empty;
                dto.ContactFullName = app.ApplicantAgent?.Name;
                dto.ContactEmail = app.ApplicantAgent?.Email;
                dto.ContactTitle = app.ApplicantAgent?.Title;
                dto.ContactBusinessPhone = app.ApplicantAgent?.Phone;
                dto.ContactCellPhone = app.ApplicantAgent?.Phone2;

                // Map tags and assignees
                if (app.ApplicationTags != null && app.ApplicationTags.Any())
                {
                    dto.ApplicationTag = _objectMapper.Map<System.Collections.Generic.List<ApplicationTags>, System.Collections.Generic.List<ApplicationTagsDto>>(app.ApplicationTags.ToList());
                }
                else
                {
                    dto.ApplicationTag = new System.Collections.Generic.List<ApplicationTagsDto>();
                }

                // Map owner
                if (app.Owner != null)
                {
                    dto.Owner = new GrantApplicationAssigneeDto
                    {
                        Id = app.Owner.Id,
                        FullName = app.Owner.FullName ?? string.Empty
                    };
                }
                else
                {
                    dto.Owner = new GrantApplicationAssigneeDto();
                }

                // Map assignees
                var assigneeDtos = new System.Collections.Generic.List<GrantApplicationAssigneeDto>();
                if (app.ApplicationAssignments != null && app.ApplicationAssignments.Count != 0)
                {
                    foreach (var assignment in app.ApplicationAssignments)
                    {
                        assigneeDtos.Add(new GrantApplicationAssigneeDto
                        {
                            ApplicationId = assignment.ApplicationId,
                            AssigneeId = assignment.AssigneeId,
                            FullName = assignment.Assignee?.FullName ?? string.Empty,
                            Id = assignment.Id,
                            Duty = assignment.Duty
                        });
                    }
                }
                dto.Assignees = assigneeDtos;

                return dto;
            }).ToList();

            var viewModel = new ApplicantSubmissionsViewModel
            {
                ApplicantId = applicantId,
                Submissions = submissionDtos
            };

            return View(viewModel);
        }
    }

    public class ApplicantSubmissionsScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files.AddIfNotContains("/js/DateUtils.js");
            context.Files.AddIfNotContains("/Views/Shared/Components/ApplicantSubmissions/Default.js");
        }
    }

    public class ApplicantSubmissionsStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files.AddIfNotContains("/Views/Shared/Components/ApplicantSubmissions/Default.css");
        }
    }
}
