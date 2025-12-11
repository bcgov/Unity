using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.Payments.Enums;
using Unity.Payments.PaymentRequests;
using Unity.Payments.Web.Views.Shared.Components.PaymentInfo;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;
using Xunit;

namespace Unity.Payments.ViewComponents
{
    public class PaymentInfoViewComponentTests : PaymentsApplicationTestBase
    {
        private readonly IAbpLazyServiceProvider _lazyServiceProvider;

        public PaymentInfoViewComponentTests()
        {
            _lazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
        }

        [Fact]
        public async Task PaymentInfo_Should_Calculate_TotalPaid_And_TotalPending_For_Current_Application()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var applicationFormVersionId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();

            var applicationDto = new GrantApplicationDto
            {
                Id = applicationId,
                RequestedAmount = 10000,
                RecommendedAmount = 8000,
                ApprovedAmount = 7000,
                Applicant = new GrantManager.GrantApplications.GrantApplicationApplicantDto { Id = applicantId }
            };

            var paymentRequests = new List<PaymentDetailsDto>
            {
                new() { Amount = 1000, Status = PaymentRequestStatus.Submitted },      // Paid
                new() { Amount = 500, Status = PaymentRequestStatus.Submitted },       // Paid
                new() { Amount = 2000, Status = PaymentRequestStatus.L1Pending },          // Pending
                new() { Amount = 1500, Status = PaymentRequestStatus.L1Declined },     // Not counted
                new() { Amount = 800, Status = PaymentRequestStatus.Paid },            // Not counted
            };

            var appService = Substitute.For<IGrantApplicationAppService>();
            var paymentRequestService = Substitute.For<IPaymentRequestAppService>();
            var featureChecker = Substitute.For<IFeatureChecker>();
            var applicationLinksService = Substitute.For<IApplicationLinksService>();

            appService.GetAsync(applicationId).Returns(applicationDto);
            paymentRequestService.GetListByApplicationIdAsync(applicationId).Returns(paymentRequests);
            featureChecker.IsEnabledAsync("Unity.Payments").Returns(true);
            applicationLinksService.GetListByApplicationAsync(applicationId).Returns([]);

            var viewComponent = CreateViewComponent(appService, paymentRequestService, featureChecker, applicationLinksService);

            // Act
            var result = await viewComponent.InvokeAsync(applicationId, applicationFormVersionId) as ViewViewComponentResult;
            var model = result!.ViewData!.Model as PaymentInfoViewModel;

            // Assert
            model.ShouldNotBeNull();
            model.TotalPaid.ShouldBe(1500m); // 1000 + 500 (Submitted only)
            model.TotalPendingAmounts.ShouldBe(3500m); // 1500 (Submitted) + 2000 (L1Pending) - excludes Paid and Declined
            model.RemainingAmount.ShouldBe(5500m); // 7000 - 1500
        }

        [Fact]
        public async Task PaymentInfo_Should_Aggregate_TotalPaid_From_Child_Applications()
        {
            // Arrange
            var parentAppId = Guid.NewGuid();
            var childApp1Id = Guid.NewGuid();
            var childApp2Id = Guid.NewGuid();
            var applicationFormVersionId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();

            var parentApplicationDto = new GrantApplicationDto
            {
                Id = parentAppId,
                ApprovedAmount = 10000,
                Applicant = new GrantManager.GrantApplications.GrantApplicationApplicantDto { Id = applicantId }
            };

            var parentPayments = new List<PaymentDetailsDto>
            {
                new() { Amount = 1000, Status = PaymentRequestStatus.Submitted }
            };

            var childApp1Payments = new List<PaymentDetailsDto>
            {
                new() { CorrelationId = childApp1Id, Amount = 500, Status = PaymentRequestStatus.Submitted }
            };

            var childApp2Payments = new List<PaymentDetailsDto>
            {
                new() { CorrelationId = childApp2Id, Amount = 800, Status = PaymentRequestStatus.Submitted }
            };

            var childLinks = new List<ApplicationLinksInfoDto>
            {
                new() { ApplicationId = childApp1Id, LinkType = ApplicationLinkType.Child },
                new() { ApplicationId = childApp2Id, LinkType = ApplicationLinkType.Child }
            };

            var allChildPayments = childApp1Payments.Concat(childApp2Payments).ToList();

            var appService = Substitute.For<IGrantApplicationAppService>();
            var paymentRequestService = Substitute.For<IPaymentRequestAppService>();
            var featureChecker = Substitute.For<IFeatureChecker>();
            var applicationLinksService = Substitute.For<IApplicationLinksService>();

            appService.GetAsync(parentAppId).Returns(parentApplicationDto);
            paymentRequestService.GetListByApplicationIdAsync(parentAppId).Returns(parentPayments);
            applicationLinksService.GetListByApplicationAsync(parentAppId).Returns(childLinks);
            paymentRequestService.GetListByApplicationIdsAsync(Arg.Any<List<Guid>>()).Returns(allChildPayments);
            featureChecker.IsEnabledAsync("Unity.Payments").Returns(true);

            var viewComponent = CreateViewComponent(appService, paymentRequestService, featureChecker, applicationLinksService);

            // Act
            var result = await viewComponent.InvokeAsync(parentAppId, applicationFormVersionId) as ViewViewComponentResult;
            var model = result!.ViewData!.Model as PaymentInfoViewModel;

            // Assert
            model.ShouldNotBeNull();
            model.TotalPaid.ShouldBe(2300m); // 1000 (parent) + 500 (child1) + 800 (child2)
        }

        [Fact]
        public async Task PaymentInfo_Should_Aggregate_TotalPendingAmounts_From_Child_Applications()
        {
            // Arrange
            var parentAppId = Guid.NewGuid();
            var childApp1Id = Guid.NewGuid();
            var childApp2Id = Guid.NewGuid();
            var applicationFormVersionId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();

            var parentApplicationDto = new GrantApplicationDto
            {
                Id = parentAppId,
                ApprovedAmount = 10000,
                Applicant = new GrantManager.GrantApplications.GrantApplicationApplicantDto { Id = applicantId }
            };

            var parentPayments = new List<PaymentDetailsDto>
            {
                new() { Amount = 2000, Status = PaymentRequestStatus.L1Pending }
            };

            var childApp1Payments = new List<PaymentDetailsDto>
            {
                new() { CorrelationId = childApp1Id, Amount = 1000, Status = PaymentRequestStatus.L1Pending }
            };

            var childApp2Payments = new List<PaymentDetailsDto>
            {
                new() { CorrelationId = childApp2Id, Amount = 500, Status = PaymentRequestStatus.L1Pending }
            };

            var childLinks = new List<ApplicationLinksInfoDto>
            {
                new() { ApplicationId = childApp1Id, LinkType = ApplicationLinkType.Child },
                new() { ApplicationId = childApp2Id, LinkType = ApplicationLinkType.Child }
            };

            var allChildPayments = childApp1Payments.Concat(childApp2Payments).ToList();

            var appService = Substitute.For<IGrantApplicationAppService>();
            var paymentRequestService = Substitute.For<IPaymentRequestAppService>();
            var featureChecker = Substitute.For<IFeatureChecker>();
            var applicationLinksService = Substitute.For<IApplicationLinksService>();

            appService.GetAsync(parentAppId).Returns(parentApplicationDto);
            paymentRequestService.GetListByApplicationIdAsync(parentAppId).Returns(parentPayments);
            applicationLinksService.GetListByApplicationAsync(parentAppId).Returns(childLinks);
            paymentRequestService.GetListByApplicationIdsAsync(Arg.Any<List<Guid>>()).Returns(allChildPayments);
            featureChecker.IsEnabledAsync("Unity.Payments").Returns(true);

            var viewComponent = CreateViewComponent(appService, paymentRequestService, featureChecker, applicationLinksService);

            // Act
            var result = await viewComponent.InvokeAsync(parentAppId, applicationFormVersionId) as ViewViewComponentResult;
            var model = result!.ViewData!.Model as PaymentInfoViewModel;

            // Assert
            model.ShouldNotBeNull();
            model.TotalPendingAmounts.ShouldBe(3500m); // 2000 (parent) + 1000 (child1) + 500 (child2)
        }

        [Fact]
        public async Task PaymentInfo_Should_Filter_Only_Child_LinkType()
        {
            // Arrange
            var parentAppId = Guid.NewGuid();
            var childAppId = Guid.NewGuid();
            var relatedAppId = Guid.NewGuid();
            var parentLinkAppId = Guid.NewGuid();
            var applicationFormVersionId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();

            var parentApplicationDto = new GrantApplicationDto
            {
                Id = parentAppId,
                ApprovedAmount = 10000,
                Applicant = new GrantManager.GrantApplications.GrantApplicationApplicantDto { Id = applicantId }
            };

            var parentPayments = new List<PaymentDetailsDto>
            {
                new() { Amount = 1000, Status = PaymentRequestStatus.Submitted }
            };

            var childPayments = new List<PaymentDetailsDto>
            {
                new() { CorrelationId = childAppId, Amount = 500, Status = PaymentRequestStatus.Submitted }
            };

            var links = new List<ApplicationLinksInfoDto>
            {
                new() { ApplicationId = childAppId, LinkType = ApplicationLinkType.Child },      // Should be included
                new() { ApplicationId = relatedAppId, LinkType = ApplicationLinkType.Related },  // Should be excluded
                new() { ApplicationId = parentLinkAppId, LinkType = ApplicationLinkType.Parent } // Should be excluded
            };

            var appService = Substitute.For<IGrantApplicationAppService>();
            var paymentRequestService = Substitute.For<IPaymentRequestAppService>();
            var featureChecker = Substitute.For<IFeatureChecker>();
            var applicationLinksService = Substitute.For<IApplicationLinksService>();

            appService.GetAsync(parentAppId).Returns(parentApplicationDto);
            paymentRequestService.GetListByApplicationIdAsync(parentAppId).Returns(parentPayments);
            applicationLinksService.GetListByApplicationAsync(parentAppId).Returns(links);
            paymentRequestService.GetListByApplicationIdsAsync(Arg.Any<List<Guid>>()).Returns(childPayments);
            featureChecker.IsEnabledAsync("Unity.Payments").Returns(true);

            var viewComponent = CreateViewComponent(appService, paymentRequestService, featureChecker, applicationLinksService);

            // Act
            var result = await viewComponent.InvokeAsync(parentAppId, applicationFormVersionId) as ViewViewComponentResult;
            var model = result!.ViewData!.Model as PaymentInfoViewModel;

            // Assert
            model.ShouldNotBeNull();
            model.TotalPaid.ShouldBe(1500m); // 1000 (parent) + 500 (only child, not related or parent links)
        }

        [Fact]
        public async Task PaymentInfo_Should_Exclude_SelfReferences()
        {
            // Arrange
            var appId = Guid.NewGuid();
            var childAppId = Guid.NewGuid();
            var applicationFormVersionId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();

            var applicationDto = new GrantApplicationDto
            {
                Id = appId,
                ApprovedAmount = 10000,
                Applicant = new GrantManager.GrantApplications.GrantApplicationApplicantDto { Id = applicantId }
            };

            var appPayments = new List<PaymentDetailsDto>
            {
                new() { Amount = 1000, Status = PaymentRequestStatus.Submitted }
            };

            var childPayments = new List<PaymentDetailsDto>
            {
                new() { CorrelationId = childAppId, Amount = 500, Status = PaymentRequestStatus.Submitted }
            };

            var links = new List<ApplicationLinksInfoDto>
            {
                new() { ApplicationId = appId, LinkType = ApplicationLinkType.Child },      // Self-reference - should be excluded
                new() { ApplicationId = childAppId, LinkType = ApplicationLinkType.Child }  // Real child - should be included
            };

            var appService = Substitute.For<IGrantApplicationAppService>();
            var paymentRequestService = Substitute.For<IPaymentRequestAppService>();
            var featureChecker = Substitute.For<IFeatureChecker>();
            var applicationLinksService = Substitute.For<IApplicationLinksService>();

            appService.GetAsync(appId).Returns(applicationDto);
            paymentRequestService.GetListByApplicationIdAsync(appId).Returns(appPayments);
            applicationLinksService.GetListByApplicationAsync(appId).Returns(links);
            paymentRequestService.GetListByApplicationIdsAsync(Arg.Any<List<Guid>>()).Returns(childPayments);
            featureChecker.IsEnabledAsync("Unity.Payments").Returns(true);

            var viewComponent = CreateViewComponent(appService, paymentRequestService, featureChecker, applicationLinksService);

            // Act
            var result = await viewComponent.InvokeAsync(appId, applicationFormVersionId) as ViewViewComponentResult;
            var model = result!.ViewData!.Model as PaymentInfoViewModel;

            // Assert
            model.ShouldNotBeNull();
            model.TotalPaid.ShouldBe(1500m); // 1000 (parent) + 500 (child only, self-reference excluded)

            // Verify that GetListByApplicationIdsAsync was called with only the child app, not the self-reference
            await paymentRequestService.Received(1).GetListByApplicationIdsAsync(
                Arg.Is<List<Guid>>(list => list.Count == 1 && list.Contains(childAppId) && !list.Contains(appId))
            );
        }

        [Fact]
        public async Task PaymentInfo_Should_Handle_NoChildApplications()
        {
            // Arrange
            var appId = Guid.NewGuid();
            var applicationFormVersionId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();

            var applicationDto = new GrantApplicationDto
            {
                Id = appId,
                RequestedAmount = 10000,
                RecommendedAmount = 8000,
                ApprovedAmount = 7000,
                Applicant = new GrantManager.GrantApplications.GrantApplicationApplicantDto { Id = applicantId }
            };

            var parentPayments = new List<PaymentDetailsDto>
            {
                new() { Amount = 1000, Status = PaymentRequestStatus.Submitted },
                new() { Amount = 2000, Status = PaymentRequestStatus.L1Pending }
            };

            var appService = Substitute.For<IGrantApplicationAppService>();
            var paymentRequestService = Substitute.For<IPaymentRequestAppService>();
            var featureChecker = Substitute.For<IFeatureChecker>();
            var applicationLinksService = Substitute.For<IApplicationLinksService>();

            appService.GetAsync(appId).Returns(applicationDto);
            paymentRequestService.GetListByApplicationIdAsync(appId).Returns(parentPayments);
            applicationLinksService.GetListByApplicationAsync(appId).Returns(new List<ApplicationLinksInfoDto>());
            featureChecker.IsEnabledAsync("Unity.Payments").Returns(true);

            var viewComponent = CreateViewComponent(appService, paymentRequestService, featureChecker, applicationLinksService);

            // Act
            var result = await viewComponent.InvokeAsync(appId, applicationFormVersionId) as ViewViewComponentResult;
            var model = result!.ViewData!.Model as PaymentInfoViewModel;

            // Assert
            model.ShouldNotBeNull();
            model.TotalPaid.ShouldBe(1000m); // Only parent payments (Submitted)
            model.TotalPendingAmounts.ShouldBe(3000m); // 1000 (Submitted) + 2000 (L1Pending)
            model.RemainingAmount.ShouldBe(6000m); // 7000 - 1000

            // Verify that GetListByApplicationIdsAsync was NOT called since there are no children
            await paymentRequestService.DidNotReceive().GetListByApplicationIdsAsync(Arg.Any<List<Guid>>());
        }

        [Fact]
        public async Task PaymentInfo_Should_Handle_ChildApplications_WithNoPaymentRequests()
        {
            // Arrange
            var parentAppId = Guid.NewGuid();
            var childAppId = Guid.NewGuid();
            var applicationFormVersionId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();

            var parentApplicationDto = new GrantApplicationDto
            {
                Id = parentAppId,
                ApprovedAmount = 10000,
                Applicant = new GrantManager.GrantApplications.GrantApplicationApplicantDto { Id = applicantId }
            };

            var parentPayments = new List<PaymentDetailsDto>
            {
                new() { Amount = 1000, Status = PaymentRequestStatus.Submitted }
            };

            var childLinks = new List<ApplicationLinksInfoDto>
            {
                new() { ApplicationId = childAppId, LinkType = ApplicationLinkType.Child }
            };

            var appService = Substitute.For<IGrantApplicationAppService>();
            var paymentRequestService = Substitute.For<IPaymentRequestAppService>();
            var featureChecker = Substitute.For<IFeatureChecker>();
            var applicationLinksService = Substitute.For<IApplicationLinksService>();

            appService.GetAsync(parentAppId).Returns(parentApplicationDto);
            paymentRequestService.GetListByApplicationIdAsync(parentAppId).Returns(parentPayments);
            applicationLinksService.GetListByApplicationAsync(parentAppId).Returns(childLinks);
            paymentRequestService.GetListByApplicationIdsAsync(Arg.Any<List<Guid>>()).Returns([]); // Empty list
            featureChecker.IsEnabledAsync("Unity.Payments").Returns(true);

            var viewComponent = CreateViewComponent(appService, paymentRequestService, featureChecker, applicationLinksService);

            // Act
            var result = await viewComponent.InvokeAsync(parentAppId, applicationFormVersionId) as ViewViewComponentResult;
            var model = result!.ViewData!.Model as PaymentInfoViewModel;

            // Assert
            model.ShouldNotBeNull();
            model.TotalPaid.ShouldBe(1000m); // Only parent payments (child has none)
            model.TotalPendingAmounts.ShouldBe(1000m); // 1000 (Submitted) - child has no payments
        }

        [Fact]
        public async Task PaymentInfo_Should_Exclude_Declined_Statuses_From_Pending()
        {
            // Arrange
            var appId = Guid.NewGuid();
            var applicationFormVersionId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();

            var applicationDto = new GrantApplicationDto
            {
                Id = appId,
                ApprovedAmount = 10000,
                Applicant = new GrantManager.GrantApplications.GrantApplicationApplicantDto { Id = applicantId }
            };

            var paymentRequests = new List<PaymentDetailsDto>
            {
                new() { Amount = 1000, Status = PaymentRequestStatus.L1Pending },          // Pending
                new() { Amount = 500, Status = PaymentRequestStatus.Paid },            // Not pending
                new() { Amount = 2000, Status = PaymentRequestStatus.L1Declined },     // Not pending
                new() { Amount = 1500, Status = PaymentRequestStatus.L2Declined },     // Not pending
                new() { Amount = 1200, Status = PaymentRequestStatus.L3Declined }      // Not pending
            };

            var appService = Substitute.For<IGrantApplicationAppService>();
            var paymentRequestService = Substitute.For<IPaymentRequestAppService>();
            var featureChecker = Substitute.For<IFeatureChecker>();
            var applicationLinksService = Substitute.For<IApplicationLinksService>();

            appService.GetAsync(appId).Returns(applicationDto);
            paymentRequestService.GetListByApplicationIdAsync(appId).Returns(paymentRequests);
            applicationLinksService.GetListByApplicationAsync(appId).Returns([]);
            featureChecker.IsEnabledAsync("Unity.Payments").Returns(true);

            var viewComponent = CreateViewComponent(appService, paymentRequestService, featureChecker, applicationLinksService);

            // Act
            var result = await viewComponent.InvokeAsync(appId, applicationFormVersionId) as ViewViewComponentResult;
            var model = result!.ViewData!.Model as PaymentInfoViewModel;

            // Assert
            model.ShouldNotBeNull();
            model.TotalPendingAmounts.ShouldBe(1000m); // Only Draft status
            model.TotalPaid.ShouldBe(0m); // No Submitted status
        }

        private PaymentInfoViewComponent CreateViewComponent(
            IGrantApplicationAppService appService,
            IPaymentRequestAppService paymentRequestService,
            IFeatureChecker featureChecker,
            IApplicationLinksService applicationLinksService)
        {
            var viewContext = new ViewContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new PaymentInfoViewComponent(
                appService, 
                paymentRequestService,
                featureChecker,
                applicationLinksService)
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = _lazyServiceProvider
            };

            return viewComponent;
        }
    }
}
