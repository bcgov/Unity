using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Applications;
using Unity.GrantManager.TestHelpers;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.PaymentRequests;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.Applicants
{
    public class PaymentInfoDataProviderTests
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly IRepository<ApplicationFormSubmission, Guid> _submissionRepo;
        private readonly IRepository<Application, Guid> _applicationRepo;
        private readonly IRepository<PaymentRequest, Guid> _paymentRequestRepo;
        private readonly PaymentInfoDataProvider _provider;

        public PaymentInfoDataProviderTests()
        {
            _currentTenant = Substitute.For<ICurrentTenant>();
            _currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());
            _submissionRepo = Substitute.For<IRepository<ApplicationFormSubmission, Guid>>();
            _applicationRepo = Substitute.For<IRepository<Application, Guid>>();
            _paymentRequestRepo = Substitute.For<IRepository<PaymentRequest, Guid>>();

            SetupEmptyQueryables();

            _provider = new PaymentInfoDataProvider(_currentTenant, _submissionRepo, _applicationRepo, _paymentRequestRepo);
        }

        private void SetupEmptyQueryables()
        {
            _submissionRepo.GetQueryableAsync()
                .Returns(Task.FromResult(Enumerable.Empty<ApplicationFormSubmission>().AsAsyncQueryable()));
            _applicationRepo.GetQueryableAsync()
                .Returns(Task.FromResult(Enumerable.Empty<Application>().AsAsyncQueryable()));
            _paymentRequestRepo.GetQueryableAsync()
                .Returns(Task.FromResult(Enumerable.Empty<PaymentRequest>().AsAsyncQueryable()));
        }

        private void SetupQueryables(
            IEnumerable<ApplicationFormSubmission> submissions,
            IEnumerable<Application> applications,
            IEnumerable<PaymentRequest> paymentRequests)
        {
            _submissionRepo.GetQueryableAsync()
                .Returns(Task.FromResult(submissions.AsAsyncQueryable()));
            _applicationRepo.GetQueryableAsync()
                .Returns(Task.FromResult(applications.AsAsyncQueryable()));
            _paymentRequestRepo.GetQueryableAsync()
                .Returns(Task.FromResult(paymentRequests.AsAsyncQueryable()));
        }

        private static ApplicantProfileInfoRequest CreateRequest() => new()
        {
            ProfileId = Guid.NewGuid(),
            Subject = "testuser@idir",
            TenantId = Guid.NewGuid(),
            Key = ApplicantProfileKeys.PaymentInfo
        };

        private static ApplicationFormSubmission CreateSubmission(Guid applicationId, string oidcSub)
        {
            var entity = new ApplicationFormSubmission { ApplicationId = applicationId, OidcSub = oidcSub };
            EntityHelper.TrySetId(entity, () => Guid.NewGuid());
            return entity;
        }

        private static Application CreateApplication(Guid id, string referenceNo = "")
        {
            var entity = new Application { ReferenceNo = referenceNo };
            EntityHelper.TrySetId(entity, () => id);
            return entity;
        }

        private static PaymentRequest CreatePaymentRequest(Guid correlationId, decimal amount = 1000m, string invoiceNumber = "INV-001", string? paymentStatus = "Paid")
        {
            var siteId = Guid.NewGuid();
            var dto = new CreatePaymentRequestDto
            {
                InvoiceNumber = invoiceNumber,
                Amount = amount,
                PayeeName = "Test Payee",
                ContractNumber = "C-001",
                SupplierNumber = "SUP-001",
                SupplierName = "Test Supplier",
                SiteId = siteId,
                CorrelationId = correlationId,
                CorrelationProvider = "Application"
            };
            var paymentRequest = new PaymentRequest(Guid.NewGuid(), dto);
            if (paymentStatus is not null)
            {
                paymentRequest.SetPaymentStatus(paymentStatus);
            }
            return paymentRequest;
        }

        [Fact]
        public async Task GetDataAsync_ShouldChangeTenant()
        {
            var request = CreateRequest();

            await _provider.GetDataAsync(request);

            _currentTenant.Received(1).Change(request.TenantId);
        }

        [Fact]
        public async Task GetDataAsync_ShouldReturnCorrectDataType()
        {
            var request = CreateRequest();

            var result = await _provider.GetDataAsync(request);

            result.DataType.ShouldBe("PAYMENTINFO");
        }

        [Fact]
        public async Task GetDataAsync_WithNoSubmissions_ShouldReturnEmptyList()
        {
            var request = CreateRequest();

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantPaymentInfoDto>();
            dto.Payments.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDataAsync_WithNullSubject_ShouldReturnEmptyDto()
        {
            var request = CreateRequest();
            request.Subject = "";

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantPaymentInfoDto>();
            dto.Payments.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDataAsync_WithNoPayments_ShouldReturnEmptyList()
        {
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [CreateApplication(applicationId, "REF-001")],
                []);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantPaymentInfoDto>();
            dto.Payments.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDataAsync_ShouldMapPaymentFields()
        {
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();

            var payment = CreatePaymentRequest(applicationId, 5000m);
            payment.SetPaymentDate("15-Jan-2025");

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [CreateApplication(applicationId, "REF-001")],
                [payment]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantPaymentInfoDto>();
            dto.Payments.Count.ShouldBe(1);

            var item = dto.Payments[0];
            item.PaymentNumber.ShouldBe("INV-001");
            item.ReferenceNo.ShouldBe("REF-001");
            item.Amount.ShouldBe(5000m);
            item.PaymentDate.ShouldBe("2025-01-15");
            item.PaymentStatus.ShouldBe("Paid");
        }

        [Fact]
        public async Task GetDataAsync_ShouldUseApplicationReferenceNo_NotPaymentReferenceNumber()
        {
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();

            var payment = CreatePaymentRequest(applicationId);

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [CreateApplication(applicationId, "APP-REF-999")],
                [payment]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantPaymentInfoDto>();
            dto.Payments[0].ReferenceNo.ShouldBe("APP-REF-999");
        }

        [Fact]
        public async Task GetDataAsync_ShouldReturnMultiplePaymentsForSameApplication()
        {
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [CreateApplication(applicationId, "REF-001")],
                [
                    CreatePaymentRequest(applicationId, 1000m),
                    CreatePaymentRequest(applicationId, 2000m)
                ]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantPaymentInfoDto>();
            dto.Payments.Count.ShouldBe(2);
        }

        [Fact]
        public async Task GetDataAsync_ShouldReturnPaymentsAcrossMultipleApplications()
        {
            var request = CreateRequest();
            var appId1 = Guid.NewGuid();
            var appId2 = Guid.NewGuid();

            SetupQueryables(
                [
                    CreateSubmission(appId1, "TESTUSER"),
                    CreateSubmission(appId2, "TESTUSER")
                ],
                [
                    CreateApplication(appId1, "REF-001"),
                    CreateApplication(appId2, "REF-002")
                ],
                [
                    CreatePaymentRequest(appId1, 1000m),
                    CreatePaymentRequest(appId2, 2000m)
                ]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantPaymentInfoDto>();
            dto.Payments.Count.ShouldBe(2);
            dto.Payments.ShouldContain(p => p.ReferenceNo == "REF-001");
            dto.Payments.ShouldContain(p => p.ReferenceNo == "REF-002");
        }

        [Fact]
        public async Task GetDataAsync_ShouldNotReturnPaymentsForOtherSubjects()
        {
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "OTHERUSER")],
                [CreateApplication(applicationId, "REF-001")],
                [CreatePaymentRequest(applicationId)]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantPaymentInfoDto>();
            dto.Payments.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDataAsync_ShouldHandleEmptyInvoiceNumber()
        {
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();

            var payment = CreatePaymentRequest(applicationId, invoiceNumber: string.Empty);

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [CreateApplication(applicationId, "REF-001")],
                [payment]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantPaymentInfoDto>();
            dto.Payments[0].PaymentNumber.ShouldBe(string.Empty);
        }

        [Fact]
        public async Task GetDataAsync_ShouldOnlyReturnPaidPayments()
        {
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [CreateApplication(applicationId, "REF-001")],
                [
                    CreatePaymentRequest(applicationId, 1000m, paymentStatus: "Paid"),
                    CreatePaymentRequest(applicationId, 2000m, paymentStatus: null),
                    CreatePaymentRequest(applicationId, 3000m, paymentStatus: "Pending"),
                    CreatePaymentRequest(applicationId, 4000m, paymentStatus: "Failed")
                ]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantPaymentInfoDto>();
            dto.Payments.Count.ShouldBe(1);
            dto.Payments[0].Amount.ShouldBe(1000m);
        }

        [Fact]
        public void Key_ShouldMatchExpected()
        {
            _provider.Key.ShouldBe(ApplicantProfileKeys.PaymentInfo);
        }

        [Fact]
        public async Task GetDataAsync_ShouldNotReturnPaymentsForUnrelatedApplications()
        {
            var request = CreateRequest();
            var matchedAppId = Guid.NewGuid();
            var unrelatedAppId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(matchedAppId, "TESTUSER")],
                [
                    CreateApplication(matchedAppId, "REF-001"),
                    CreateApplication(unrelatedAppId, "REF-999")
                ],
                [
                    CreatePaymentRequest(matchedAppId, 1000m),
                    CreatePaymentRequest(unrelatedAppId, 5000m)
                ]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantPaymentInfoDto>();
            dto.Payments.Count.ShouldBe(1);
            dto.Payments[0].ReferenceNo.ShouldBe("REF-001");
        }
    }
}
