using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
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
        public async Task PaymentInfo_Should_Display_TotalPaid_And_TotalPending_From_Rollup()
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
                Applicant = new GrantApplicationApplicantDto { Id = applicantId }
            };

            var rollup = new ApplicationPaymentRollupDto
            {
                ApplicationId = applicationId,
                TotalPaid = 1500m,
                TotalPending = 2000m
            };

            var appService = Substitute.For<IGrantApplicationAppService>();
            var paymentRequestService = Substitute.For<IPaymentRequestAppService>();
            var featureChecker = Substitute.For<IFeatureChecker>();

            appService.GetAsync(applicationId).Returns(applicationDto);
            paymentRequestService.GetApplicationPaymentRollupAsync(applicationId).Returns(rollup);
            featureChecker.IsEnabledAsync("Unity.Payments").Returns(true);

            var viewComponent = CreateViewComponent(appService, paymentRequestService, featureChecker);

            // Act
            var result = await viewComponent.InvokeAsync(applicationId, applicationFormVersionId) as ViewViewComponentResult;
            var model = result!.ViewData!.Model as PaymentInfoViewModel;

            // Assert
            model.ShouldNotBeNull();
            model.TotalPaid.ShouldBe(1500m);
            model.TotalPendingAmounts.ShouldBe(2000m);
            model.RemainingAmount.ShouldBe(5500m); // 7000 - 1500
        }

        [Fact]
        public async Task PaymentInfo_Should_Calculate_RemainingAmount_From_ApprovedAmount_Minus_TotalPaid()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var applicationFormVersionId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();

            var applicationDto = new GrantApplicationDto
            {
                Id = applicationId,
                ApprovedAmount = 10000,
                Applicant = new GrantApplicationApplicantDto { Id = applicantId }
            };

            var rollup = new ApplicationPaymentRollupDto
            {
                ApplicationId = applicationId,
                TotalPaid = 3500m,
                TotalPending = 1000m
            };

            var appService = Substitute.For<IGrantApplicationAppService>();
            var paymentRequestService = Substitute.For<IPaymentRequestAppService>();
            var featureChecker = Substitute.For<IFeatureChecker>();

            appService.GetAsync(applicationId).Returns(applicationDto);
            paymentRequestService.GetApplicationPaymentRollupAsync(applicationId).Returns(rollup);
            featureChecker.IsEnabledAsync("Unity.Payments").Returns(true);

            var viewComponent = CreateViewComponent(appService, paymentRequestService, featureChecker);

            // Act
            var result = await viewComponent.InvokeAsync(applicationId, applicationFormVersionId) as ViewViewComponentResult;
            var model = result!.ViewData!.Model as PaymentInfoViewModel;

            // Assert
            model.ShouldNotBeNull();
            model.RemainingAmount.ShouldBe(6500m); // 10000 - 3500
        }

        [Fact]
        public async Task PaymentInfo_Should_Include_Child_Application_Amounts_By_Rollup()
        {
            // The ViewComponent now delegates child aggregation to the service layer.
            // This test verifies it correctly uses the pre-aggregated rollup.
            // Arrange
            var parentAppId = Guid.NewGuid();
            var applicationFormVersionId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();

            var applicationDto = new GrantApplicationDto
            {
                Id = parentAppId,
                ApprovedAmount = 10000,
                Applicant = new GrantApplicationApplicantDto { Id = applicantId }
            };

            // Rollup includes parent + child amounts (pre-aggregated by service)
            var rollup = new ApplicationPaymentRollupDto
            {
                ApplicationId = parentAppId,
                TotalPaid = 2300m,   // e.g., 1000 (parent) + 500 (child1) + 800 (child2)
                TotalPending = 3500m // e.g., 2000 (parent) + 1000 (child1) + 500 (child2)
            };

            var appService = Substitute.For<IGrantApplicationAppService>();
            var paymentRequestService = Substitute.For<IPaymentRequestAppService>();
            var featureChecker = Substitute.For<IFeatureChecker>();

            appService.GetAsync(parentAppId).Returns(applicationDto);
            paymentRequestService.GetApplicationPaymentRollupAsync(parentAppId).Returns(rollup);
            featureChecker.IsEnabledAsync("Unity.Payments").Returns(true);

            var viewComponent = CreateViewComponent(appService, paymentRequestService, featureChecker);

            // Act
            var result = await viewComponent.InvokeAsync(parentAppId, applicationFormVersionId) as ViewViewComponentResult;
            var model = result!.ViewData!.Model as PaymentInfoViewModel;

            // Assert
            model.ShouldNotBeNull();
            model.TotalPaid.ShouldBe(2300m);
            model.TotalPendingAmounts.ShouldBe(3500m);
            model.RemainingAmount.ShouldBe(7700m); // 10000 - 2300
        }

        [Fact]
        public async Task PaymentInfo_Should_Handle_Zero_Payments()
        {
            // Arrange
            var appId = Guid.NewGuid();
            var applicationFormVersionId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();

            var applicationDto = new GrantApplicationDto
            {
                Id = appId,
                ApprovedAmount = 5000,
                Applicant = new GrantApplicationApplicantDto { Id = applicantId }
            };

            var rollup = new ApplicationPaymentRollupDto
            {
                ApplicationId = appId,
                TotalPaid = 0m,
                TotalPending = 0m
            };

            var appService = Substitute.For<IGrantApplicationAppService>();
            var paymentRequestService = Substitute.For<IPaymentRequestAppService>();
            var featureChecker = Substitute.For<IFeatureChecker>();

            appService.GetAsync(appId).Returns(applicationDto);
            paymentRequestService.GetApplicationPaymentRollupAsync(appId).Returns(rollup);
            featureChecker.IsEnabledAsync("Unity.Payments").Returns(true);

            var viewComponent = CreateViewComponent(appService, paymentRequestService, featureChecker);

            // Act
            var result = await viewComponent.InvokeAsync(appId, applicationFormVersionId) as ViewViewComponentResult;
            var model = result!.ViewData!.Model as PaymentInfoViewModel;

            // Assert
            model.ShouldNotBeNull();
            model.TotalPaid.ShouldBe(0m);
            model.TotalPendingAmounts.ShouldBe(0m);
            model.RemainingAmount.ShouldBe(5000m); // 5000 - 0
        }

        [Fact]
        public async Task PaymentInfo_Should_Map_RequestedAmount_And_RecommendedAmount()
        {
            // Arrange
            var appId = Guid.NewGuid();
            var applicationFormVersionId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();

            var applicationDto = new GrantApplicationDto
            {
                Id = appId,
                RequestedAmount = 15000,
                RecommendedAmount = 12000,
                ApprovedAmount = 10000,
                Applicant = new GrantApplicationApplicantDto { Id = applicantId }
            };

            var rollup = new ApplicationPaymentRollupDto
            {
                ApplicationId = appId,
                TotalPaid = 0m,
                TotalPending = 0m
            };

            var appService = Substitute.For<IGrantApplicationAppService>();
            var paymentRequestService = Substitute.For<IPaymentRequestAppService>();
            var featureChecker = Substitute.For<IFeatureChecker>();

            appService.GetAsync(appId).Returns(applicationDto);
            paymentRequestService.GetApplicationPaymentRollupAsync(appId).Returns(rollup);
            featureChecker.IsEnabledAsync("Unity.Payments").Returns(true);

            var viewComponent = CreateViewComponent(appService, paymentRequestService, featureChecker);

            // Act
            var result = await viewComponent.InvokeAsync(appId, applicationFormVersionId) as ViewViewComponentResult;
            var model = result!.ViewData!.Model as PaymentInfoViewModel;

            // Assert
            model.ShouldNotBeNull();
            model.RequestedAmount.ShouldBe(15000);
            model.RecommendedAmount.ShouldBe(12000);
            model.ApprovedAmount.ShouldBe(10000);
            model.ApplicationId.ShouldBe(appId);
            model.ApplicationFormVersionId.ShouldBe(applicationFormVersionId);
            model.ApplicantId.ShouldBe(applicantId);
        }

        [Fact]
        public async Task PaymentInfo_Should_Return_Empty_View_When_Feature_Disabled()
        {
            // Arrange
            var appId = Guid.NewGuid();
            var applicationFormVersionId = Guid.NewGuid();

            var appService = Substitute.For<IGrantApplicationAppService>();
            var paymentRequestService = Substitute.For<IPaymentRequestAppService>();
            var featureChecker = Substitute.For<IFeatureChecker>();

            featureChecker.IsEnabledAsync("Unity.Payments").Returns(false);

            var viewComponent = CreateViewComponent(appService, paymentRequestService, featureChecker);

            // Act
            var result = await viewComponent.InvokeAsync(appId, applicationFormVersionId) as ViewViewComponentResult;
            var model = result!.ViewData!.Model as PaymentInfoViewModel;

            // Assert
            model.ShouldNotBeNull();
            model.TotalPaid.ShouldBeNull();
            model.TotalPendingAmounts.ShouldBeNull();
            model.RemainingAmount.ShouldBeNull();

            // Verify no service calls were made
            await appService.DidNotReceive().GetAsync(Arg.Any<Guid>());
            await paymentRequestService.DidNotReceive().GetApplicationPaymentRollupAsync(Arg.Any<Guid>());
        }

        [Fact]
        public async Task PaymentInfo_Should_Call_GetApplicationPaymentRollupAsync_With_ApplicationId()
        {
            // Arrange
            var appId = Guid.NewGuid();
            var applicationFormVersionId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();

            var applicationDto = new GrantApplicationDto
            {
                Id = appId,
                ApprovedAmount = 5000,
                Applicant = new GrantApplicationApplicantDto { Id = applicantId }
            };

            var rollup = new ApplicationPaymentRollupDto
            {
                ApplicationId = appId,
                TotalPaid = 100m,
                TotalPending = 200m
            };

            var appService = Substitute.For<IGrantApplicationAppService>();
            var paymentRequestService = Substitute.For<IPaymentRequestAppService>();
            var featureChecker = Substitute.For<IFeatureChecker>();

            appService.GetAsync(appId).Returns(applicationDto);
            paymentRequestService.GetApplicationPaymentRollupAsync(appId).Returns(rollup);
            featureChecker.IsEnabledAsync("Unity.Payments").Returns(true);

            var viewComponent = CreateViewComponent(appService, paymentRequestService, featureChecker);

            // Act
            await viewComponent.InvokeAsync(appId, applicationFormVersionId);

            // Assert - Verify the correct service method was called with the right ID
            await paymentRequestService.Received(1).GetApplicationPaymentRollupAsync(appId);
        }

        private PaymentInfoViewComponent CreateViewComponent(
            IGrantApplicationAppService appService,
            IPaymentRequestAppService paymentRequestService,
            IFeatureChecker featureChecker)
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
                featureChecker)
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = _lazyServiceProvider
            };

            return viewComponent;
        }
    }
}
