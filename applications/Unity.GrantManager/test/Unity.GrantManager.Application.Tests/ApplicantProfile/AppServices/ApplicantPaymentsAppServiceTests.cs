using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.Payments.Enums;
using Unity.Payments.PaymentRequests;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace Unity.GrantManager.ApplicantProfile
{
    public class ApplicantPaymentsAppServiceTests
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IPaymentRequestAppService _paymentRequestAppService;
        private readonly ApplicantPaymentsAppService _service;

        public ApplicantPaymentsAppServiceTests()
        {
            _applicationRepository = Substitute.For<IApplicationRepository>();
            _paymentRequestAppService = Substitute.For<IPaymentRequestAppService>();

            _service = new ApplicantPaymentsAppService(
                _applicationRepository,
                _paymentRequestAppService);

            _paymentRequestAppService
                .GetListByApplicationIdsAsync(Arg.Any<List<Guid>>())
                .Returns([]);
        }

        private static Application CreateApplication(decimal approvedAmount, Collection<ApplicationLink>? links = null)
        {
            var application = new Application
            {
                ApprovedAmount = approvedAmount,
                ApplicationLinks = links
            };
            EntityHelper.TrySetId(application, () => Guid.NewGuid());
            return application;
        }

        [Fact]
        public async Task GetPaymentSummaryByApplicantIdAsync_WithNoLinks_ShouldSumAllApplications()
        {
            // Arrange
            var applicantId = Guid.NewGuid();
            var applications = new List<Application>
            {
                CreateApplication(1000m),
                CreateApplication(500m)
            };
            _applicationRepository.GetByApplicantIdAsync(applicantId).Returns(applications);

            // Act
            var result = await _service.GetPaymentSummaryByApplicantIdAsync(applicantId);

            // Assert
            result.TotalApprovedAmount.ShouldBe(1500m);
        }

        [Fact]
        public async Task GetPaymentSummaryByApplicantIdAsync_ShouldExcludeChildApplications_FromTotalApprovedAmount()
        {
            // Arrange
            var applicantId = Guid.NewGuid();
            var parentApplication = CreateApplication(1000m);
            var childApplication = CreateApplication(400m, [
                new ApplicationLink
                {
                    ApplicationId = Guid.NewGuid(),
                    LinkedApplicationId = parentApplication.Id,
                    LinkType = ApplicationLinkType.Parent
                }
            ]);
            var applications = new List<Application> { parentApplication, childApplication };
            _applicationRepository.GetByApplicantIdAsync(applicantId).Returns(applications);

            // Act
            var result = await _service.GetPaymentSummaryByApplicantIdAsync(applicantId);

            // Assert
            result.TotalApprovedAmount.ShouldBe(1000m);
        }

        [Fact]
        public async Task GetPaymentSummaryByApplicantIdAsync_WithChildTypeLink_ShouldNotExcludeParentApplication()
        {
            // Arrange
            // A "Child"-type link on an application's own ApplicationLinks means the linked
            // application is ITS child - i.e. this application is the parent, and should still
            // be included in the total.
            var applicantId = Guid.NewGuid();
            var parentApplication = CreateApplication(1000m, [
                new ApplicationLink
                {
                    ApplicationId = Guid.NewGuid(),
                    LinkedApplicationId = Guid.NewGuid(),
                    LinkType = ApplicationLinkType.Child
                }
            ]);
            var applications = new List<Application> { parentApplication };
            _applicationRepository.GetByApplicantIdAsync(applicantId).Returns(applications);

            // Act
            var result = await _service.GetPaymentSummaryByApplicantIdAsync(applicantId);

            // Assert
            result.TotalApprovedAmount.ShouldBe(1000m);
        }

        [Fact]
        public async Task GetPaymentSummaryByApplicantIdAsync_WithRelatedTypeLink_ShouldNotExcludeApplication()
        {
            // Arrange
            var applicantId = Guid.NewGuid();
            var relatedApplication = CreateApplication(750m, [
                new ApplicationLink
                {
                    ApplicationId = Guid.NewGuid(),
                    LinkedApplicationId = Guid.NewGuid(),
                    LinkType = ApplicationLinkType.Related
                }
            ]);
            var applications = new List<Application> { relatedApplication };
            _applicationRepository.GetByApplicantIdAsync(applicantId).Returns(applications);

            // Act
            var result = await _service.GetPaymentSummaryByApplicantIdAsync(applicantId);

            // Assert
            result.TotalApprovedAmount.ShouldBe(750m);
        }

        [Fact]
        public async Task GetPaymentSummaryByApplicantIdAsync_ShouldStillIncludePaymentsForChildApplications_InTotalPaidAmount()
        {
            // Arrange: TotalPaidAmount/TotalRemainingAmount are unaffected by the child-exclusion
            // change - they remain based on payments across all applications, including children.
            var applicantId = Guid.NewGuid();
            var parentApplication = CreateApplication(1000m);
            var childApplication = CreateApplication(400m, [
                new ApplicationLink
                {
                    ApplicationId = Guid.NewGuid(),
                    LinkedApplicationId = parentApplication.Id,
                    LinkType = ApplicationLinkType.Parent
                }
            ]);
            var applications = new List<Application> { parentApplication, childApplication };
            _applicationRepository.GetByApplicantIdAsync(applicantId).Returns(applications);

            _paymentRequestAppService
                .GetListByApplicationIdsAsync(Arg.Any<List<Guid>>())
                .Returns(
                [
                    new PaymentDetailsDto
                    {
                        CorrelationId = childApplication.Id,
                        Amount = 400m,
                        Status = PaymentRequestStatus.HistoricalPayment
                    }
                ]);

            // Act
            var result = await _service.GetPaymentSummaryByApplicantIdAsync(applicantId);

            // Assert
            result.TotalApprovedAmount.ShouldBe(1000m);
            result.TotalPaidAmount.ShouldBe(400m);
            result.TotalRemainingAmount.ShouldBe(600m);
        }

        [Fact]
        public async Task GetPaymentSummaryByApplicantIdAsync_WithNoApplications_ShouldReturnZeroApproved()
        {
            // Arrange
            var applicantId = Guid.NewGuid();
            _applicationRepository.GetByApplicantIdAsync(applicantId).Returns([]);

            // Act
            var result = await _service.GetPaymentSummaryByApplicantIdAsync(applicantId);

            // Assert
            result.TotalApprovedAmount.ShouldBe(0m);
        }
    }
}
