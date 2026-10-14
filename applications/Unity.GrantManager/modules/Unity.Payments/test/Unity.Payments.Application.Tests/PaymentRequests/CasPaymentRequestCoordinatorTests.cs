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

    private static CasPaymentRequestCoordinator CreateCoordinator(
        IPaymentRequestRepository repository,
        IUnitOfWork unitOfWork)
    {
        var unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        unitOfWorkManager.Begin(Arg.Any<AbpUnitOfWorkOptions>(), Arg.Any<bool>()).Returns(unitOfWork);

        var coordinator = new TestableCasPaymentRequestCoordinator(
            paymentQueueService: null!,
            paymentRequestsRepository: repository,
            unitOfWorkManager: unitOfWorkManager,
            tenantRepository: Substitute.For<ITenantRepository>(),
            currentTenant: Substitute.For<ICurrentTenant>(),
            localEventBus: Substitute.For<ILocalEventBus>());

        var lazyServiceProvider = Substitute.For<IAbpLazyServiceProvider>();
        lazyServiceProvider.LazyGetService<ILogger>(Arg.Any<Func<IServiceProvider, ILogger>>())
            .Returns(NullLogger<CasPaymentRequestCoordinator>.Instance);
        typeof(ApplicationService)
            .GetProperty("LazyServiceProvider", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(coordinator, lazyServiceProvider);
        return coordinator;
    }

    private sealed class TestableCasPaymentRequestCoordinator(
        PaymentQueueService paymentQueueService,
        IPaymentRequestRepository paymentRequestsRepository,
        IUnitOfWorkManager unitOfWorkManager,
        ITenantRepository tenantRepository,
        ICurrentTenant currentTenant,
        ILocalEventBus localEventBus)
        : CasPaymentRequestCoordinator(
            paymentQueueService,
            paymentRequestsRepository,
            unitOfWorkManager,
            tenantRepository,
            currentTenant,
            localEventBus)
    { }

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
