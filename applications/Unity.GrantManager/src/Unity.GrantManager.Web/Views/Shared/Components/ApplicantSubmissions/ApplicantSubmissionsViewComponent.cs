using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Payments;
using Unity.Payments.Enums;
using Unity.Payments.PaymentRequests;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.Features;
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
        private readonly IFeatureChecker _featureChecker;
        private readonly IPaymentRequestAppService _paymentRequestService;

        public ApplicantSubmissionsViewComponent(
            IApplicationRepository applicationRepository,
            IObjectMapper objectMapper,
            IFeatureChecker featureChecker,
            IPaymentRequestAppService paymentRequestService)
        {
            _applicationRepository = applicationRepository;
            _objectMapper = objectMapper;
            _featureChecker = featureChecker;
            _paymentRequestService = paymentRequestService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicantId)
        {
            if (applicantId == Guid.Empty)
            {
                return View(new ApplicantSubmissionsViewModel
                {
                    ApplicantId = applicantId,
                    Submissions = []
                });
            }

            // Query applications
            var applications = await _applicationRepository.GetByApplicantIdAsync(applicantId);

            var applicationIds = applications.Select(app => app.Id).ToList();
            var paymentsFeatureEnabled = await _featureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature);

            Dictionary<Guid, decimal> paymentRequestsByApplication = [];
            if (paymentsFeatureEnabled && applicationIds.Count > 0)
            {
                var paymentRequests = await _paymentRequestService.GetListByApplicationIdsAsync(applicationIds);
                paymentRequestsByApplication = paymentRequests
                    .Where(pr => pr.Status == PaymentRequestStatus.Submitted)
                    .GroupBy(pr => pr.CorrelationId)
                    .ToDictionary(g => g.Key, g => g.Sum(pr => pr.Amount));
            }

            // Map to DTOs (similar to GrantApplicationAppService.GetListAsync)
            var submissionDtos = applications.Select(app =>
            {
                var dto = _objectMapper.Map<Application, GrantApplicationDto>(app);

                // Map related entities
                dto.Status = app.ApplicationStatus?.InternalStatus ?? string.Empty;
                dto.Category = app.ApplicationForm?.Category ?? string.Empty;
                dto.SubStatusDisplayValue = MapSubstatusDisplayValue(dto.SubStatus);
                dto.DeclineRational = MapDeclineRationalDisplayValue(dto.DeclineRational);
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
                if (app.ApplicationTags != null && app.ApplicationTags.Count != 0)
                {
                    dto.ApplicationTag = _objectMapper.Map<System.Collections.Generic.List<ApplicationTags>, System.Collections.Generic.List<ApplicationTagsDto>>([.. app.ApplicationTags]);
                }
                else
                {
                    dto.ApplicationTag = [];
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

                if (paymentsFeatureEnabled && paymentRequestsByApplication.Count > 0)
                {
                    paymentRequestsByApplication.TryGetValue(app.Id, out var totalPaid);
                    dto.PaymentInfo = new PaymentInfoDto
                    {
                        ApprovedAmount = app.ApprovedAmount,
                        TotalPaid = totalPaid
                    };
                }

                return dto;
            }).ToList();

            var viewModel = new ApplicantSubmissionsViewModel
            {
                ApplicantId = applicantId,
                Submissions = submissionDtos
            };

            return View(viewModel);
        }

        private static string MapSubstatusDisplayValue(string subStatus)
        {
            if (string.IsNullOrWhiteSpace(subStatus)) return string.Empty;
            return AssessmentResultsOptionsList.SubStatusActionList.TryGetValue(subStatus, out var value)
                ? value ?? string.Empty
                : string.Empty;
        }

        private static string MapDeclineRationalDisplayValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return AssessmentResultsOptionsList.DeclineRationalActionList.TryGetValue(value, out var displayValue)
                ? displayValue ?? string.Empty
                : string.Empty;
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
