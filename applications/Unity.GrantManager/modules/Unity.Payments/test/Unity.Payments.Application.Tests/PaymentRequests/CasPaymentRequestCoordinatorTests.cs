using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Microsoft.Extensions.Logging.Abstractions;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Integrations.Cas;
using Unity.Payments.Enums;
using Unity.Payments.Events;
using Unity.Notifications.Integrations.RabbitMQ;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;
using Xunit;

namespace Unity.Payments.PaymentRequests;

public class CasPaymentRequestCoordinatorTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid PaymentRequestId = Guid.NewGuid();

    [Fact]
    public async Task UpdatePaymentRequestStatus_RetriesAfterConcurrencyConflict()
    {
        var repository = Substitute.For<IPaymentRequestRepository>();
        repository.GetAsync(PaymentRequestId)
            .Returns(CreatePaymentRequest(), CreatePaymentRequest(), CreatePaymentRequest());
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.CompleteAsync()
            .Returns(Task.FromException(new AbpDbConcurrencyException()), Task.CompletedTask);
        var coordinator = CreateCoordinator(repository, unitOfWork);

        var result = await coordinator.UpdatePaymentRequestStatus(TenantId, PaymentRequestId, CreateCasResult());

        result.ShouldNotBeNull();
        result!.PaymentStatus.ShouldBe("Paid");
        _ = repository.Received(2).UpdateAsync(Arg.Any<PaymentRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        _ = unitOfWork.Received(2).CompleteAsync();
    }

    [Fact]
    public async Task UpdatePaymentRequestStatus_ReturnsCurrentRequestWhenResultIsAlreadyApplied()
    {
        var repository = Substitute.For<IPaymentRequestRepository>();
        repository.GetAsync(PaymentRequestId)
            .Returns(CreatePaymentRequest(), CreatePaymentRequest(paymentStatus: "Paid"));
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.CompleteAsync().Returns(Task.FromException(new AbpDbConcurrencyException()));
        var coordinator = CreateCoordinator(repository, unitOfWork);

        var result = await coordinator.UpdatePaymentRequestStatus(TenantId, PaymentRequestId, CreateCasResult());

        result!.PaymentStatus.ShouldBe("Paid");
        _ = repository.Received(1).UpdateAsync(Arg.Any<PaymentRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        _ = unitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task UpdatePaymentRequestStatus_RejectsStaleCasResult()
    {
        var repository = Substitute.For<IPaymentRequestRepository>();
        repository.GetAsync(PaymentRequestId)
            .Returns(CreatePaymentRequest(), CreatePaymentRequest(invoiceStatus: "Paid"));
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.CompleteAsync().Returns(Task.FromException(new AbpDbConcurrencyException()));
        var coordinator = CreateCoordinator(repository, unitOfWork);

        var result = await coordinator.UpdatePaymentRequestStatus(TenantId, PaymentRequestId, CreateCasResult());

        result!.InvoiceStatus.ShouldBe("Paid");
        _ = repository.Received(1).UpdateAsync(Arg.Any<PaymentRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        _ = unitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task UpdatePaymentRequestStatus_RejectsStateChangedBeforeRetryLoad()
    {
        var repository = Substitute.For<IPaymentRequestRepository>();
        repository.GetAsync(PaymentRequestId)
            .Returns(
                CreatePaymentRequest(),
                CreatePaymentRequest(),
                CreatePaymentRequest(invoiceStatus: "Paid"));
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.CompleteAsync().Returns(Task.FromException(new AbpDbConcurrencyException()));
        var coordinator = CreateCoordinator(repository, unitOfWork);

        var result = await coordinator.UpdatePaymentRequestStatus(TenantId, PaymentRequestId, CreateCasResult());

        result!.InvoiceStatus.ShouldBe("Paid");
        _ = repository.Received(1).UpdateAsync(Arg.Any<PaymentRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        _ = unitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task UpdatePaymentRequestStatus_ThrowsAfterMaximumConcurrencyAttempts()
    {
        var repository = Substitute.For<IPaymentRequestRepository>();
        repository.GetAsync(PaymentRequestId)
            .Returns(
                CreatePaymentRequest(),
                CreatePaymentRequest(),
                CreatePaymentRequest(),
                CreatePaymentRequest(),
                CreatePaymentRequest());
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.CompleteAsync().Returns(
            Task.FromException(new AbpDbConcurrencyException()),
            Task.FromException(new AbpDbConcurrencyException()),
            Task.FromException(new AbpDbConcurrencyException()));
        var coordinator = CreateCoordinator(repository, unitOfWork);

        await Should.ThrowAsync<AbpDbConcurrencyException>(() =>
            coordinator.UpdatePaymentRequestStatus(TenantId, PaymentRequestId, CreateCasResult()));

        _ = unitOfWork.Received(3).CompleteAsync();
    }

    [Theory]
    [InlineData(PaymentRequestStatus.FSB, null)]
    [InlineData(PaymentRequestStatus.FSB, "")]
    [InlineData(PaymentRequestStatus.Submitted, null)]
    [InlineData(PaymentRequestStatus.Submitted, "")]
    public async Task Should_NotPublishStatusChange_WhenReconciliationOnlyNormalizesMissingCasStatus(
        PaymentRequestStatus workflowStatus,
        string? paymentStatus)
    {
        var paymentRequest = CreatePaymentRequest(paymentStatus: paymentStatus);
        paymentRequest.SetPaymentRequestStatus(workflowStatus);
        var repository = Substitute.For<IPaymentRequestRepository>();
        repository.GetAsync(PaymentRequestId).Returns(paymentRequest);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.CompleteAsync().Returns(Task.CompletedTask);
        var eventBus = Substitute.For<ILocalEventBus>();
        var coordinator = CreateCoordinator(repository, unitOfWork, eventBus);

        var result = await coordinator.UpdatePaymentRequestStatus(TenantId, PaymentRequestId,
            new CasPaymentSearchResult { InvoiceStatus = "NotFound" });

        result!.Status.ShouldBe(workflowStatus);
        result.PaymentStatus.ShouldBeEmpty();
        result.InvoiceStatus.ShouldBe("NotFound");
        await eventBus.DidNotReceive().PublishAsync(Arg.Any<PaymentStatusChangedEvent>(), Arg.Any<bool>());
    }

    [Theory]
    [InlineData(PaymentRequestStatus.FSB, null)]
    [InlineData(PaymentRequestStatus.FSB, "Not Paid")]
    [InlineData(PaymentRequestStatus.Submitted, null)]
    [InlineData(PaymentRequestStatus.Submitted, "Not Paid")]
    public async Task Should_PublishPreviousStatuses_OnlyWhenCasStatusChanges(
        PaymentRequestStatus workflowStatus,
        string? previousCasPaymentStatus)
    {
        var paymentRequest = CreatePaymentRequest(paymentStatus: previousCasPaymentStatus);
        paymentRequest.SetPaymentRequestStatus(workflowStatus);
        var repository = Substitute.For<IPaymentRequestRepository>();
        repository.GetAsync(PaymentRequestId).Returns(paymentRequest);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.CompleteAsync().Returns(Task.CompletedTask);
        var eventBus = Substitute.For<ILocalEventBus>();
        var coordinator = CreateCoordinator(repository, unitOfWork, eventBus);
        var casResult = new CasPaymentSearchResult
        {
            InvoiceStatus = "Validated",
            PaymentStatus = "Fully Paid"
        };

        await coordinator.UpdatePaymentRequestStatus(TenantId, PaymentRequestId, casResult);
        await coordinator.UpdatePaymentRequestStatus(TenantId, PaymentRequestId, casResult);

        await eventBus.Received(1).PublishAsync(Arg.Is<PaymentStatusChangedEvent>(e =>
            e.PaymentRequestId == PaymentRequestId &&
            e.TenantId == TenantId &&
            e.PreviousStatus == workflowStatus &&
            e.Status == workflowStatus &&
            e.PreviousCasPaymentStatus == previousCasPaymentStatus &&
            e.CasPaymentStatus == "Fully Paid"), Arg.Any<bool>());
    }

    private static CasPaymentRequestCoordinator CreateCoordinator(
        IPaymentRequestRepository repository,
        IUnitOfWork unitOfWork,
        ILocalEventBus? eventBus = null)
    {
        var unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        unitOfWorkManager.Begin(Arg.Any<AbpUnitOfWorkOptions>(), Arg.Any<bool>()).Returns(unitOfWork);

        var coordinator = new CasPaymentRequestCoordinator(
            paymentQueueService: null!,
            paymentRequestsRepository: repository,
            unitOfWorkManager: unitOfWorkManager,
            tenantRepository: Substitute.For<ITenantRepository>(),
            currentTenant: Substitute.For<ICurrentTenant>(),
            localEventBus: eventBus ?? Substitute.For<ILocalEventBus>());

        var lazyServiceProvider = Substitute.For<IAbpLazyServiceProvider>();
        lazyServiceProvider.LazyGetService<ILogger>(Arg.Any<Func<IServiceProvider, ILogger>>())
            .Returns(NullLogger<CasPaymentRequestCoordinator>.Instance);
        typeof(ApplicationService)
            .GetProperty("LazyServiceProvider", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(coordinator, lazyServiceProvider);
        return coordinator;
    }

    private static PaymentRequest CreatePaymentRequest(
        string? invoiceStatus = null,
        string? paymentStatus = null)
    {
        var paymentRequest = new PaymentRequest(PaymentRequestId, new CreatePaymentRequestDto
        {
            InvoiceNumber = "INV-123",
            Amount = 100,
            PayeeName = "Supplier",
            ContractNumber = "CON-123",
            SupplierNumber = "SUP-123",
            SupplierName = "Supplier",
            CorrelationId = Guid.NewGuid(),
            SiteId = Guid.NewGuid(),
            AccountCodingId = Guid.NewGuid(),
            BatchName = "Batch"
        });

        if (invoiceStatus != null)
        {
            paymentRequest.SetInvoiceStatus(invoiceStatus);
        }

        if (paymentStatus != null)
        {
            paymentRequest.SetPaymentStatus(paymentStatus);
        }

        return paymentRequest;
    }

    private static CasPaymentSearchResult CreateCasResult()
    {
        return new CasPaymentSearchResult
        {
            InvoiceStatus = "Paid",
            PaymentStatus = "Paid",
            PaymentNumber = "PAY-123",
            PaymentDate = "2026-10-13"
        };
    }
}
