using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Domain.Suppliers.ValueObjects;
using Unity.Payments.Enums;
using Volo.Abp.Uow;
using Xunit;

namespace Unity.Payments.PaymentRequests;

public class PaymentRequestAppService_Tests : PaymentsApplicationTestBase
{
    private readonly IPaymentRequestAppService _paymentRequestAppService;
    private readonly IPaymentRequestRepository _paymentRequestRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public PaymentRequestAppService_Tests()
    {
        _paymentRequestAppService = GetRequiredService<IPaymentRequestAppService>();
        _paymentRequestRepository = GetRequiredService<IPaymentRequestRepository>();
        _supplierRepository = GetRequiredService<ISupplierRepository>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateAsync_CreatesPaymentRequest()
    {
        // Arrange        
        using var uow = _unitOfWorkManager.Begin();
        var siteId = Guid.NewGuid();
        var newSupplier = new Supplier(Guid.NewGuid(),
            "Supplier",
            "123",
            new Modules.Shared.Correlation.Correlation(Guid.NewGuid(), "Test"),
            new MailingAddress(
            "Address1",
            "City",
            "Province",
            "ABC123"));

        newSupplier.AddSite(new Site(siteId,
            "123",
            PaymentGroup.EFT,
            new Address(
            "123",
            "456",
            "789",
            "Country",
            "City",
            "Province",
            "ABC123")));

        _ = await _supplierRepository.InsertAsync(newSupplier, true);

        List<CreatePaymentRequestDto> paymentRequests =
        [
            new()
            {
               Amount = 50,
               InvoiceNumber ="Test",
               ContractNumber ="",
               CorrelationId = Guid.NewGuid(),
               Description = "",
               PayeeName= "",
               SiteId= siteId,
               SupplierNumber = "",
            }
        ];
        // Act        
        var insertedPaymentRequest = await _paymentRequestAppService
            .CreateAsync(paymentRequests);

        // Assert
        var paymentRequest = await _paymentRequestRepository.GetAsync(insertedPaymentRequest[0].Id, true);
        paymentRequest.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_ReturnsPaymentsList()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var supplier = new Supplier(Guid.NewGuid(), "supp", "123", new Modules.Shared.Correlation.Correlation(Guid.NewGuid(), "A"));
        supplier.AddSite(new Site(Guid.NewGuid(), "123", PaymentGroup.EFT));
        var addedSupplier = await _supplierRepository.InsertAsync(supplier);
        CreatePaymentRequestDto paymentRequestDto = new()
        {
            InvoiceNumber = "",
            Amount = 100,
            PayeeName = "Test",
            ContractNumber = "0000000000",
            SupplierNumber = "",
            SiteId = addedSupplier.Sites[0].Id,
            CorrelationId = Guid.NewGuid(),
            CorrelationProvider = "",
            ReferenceNumber = "UP-XXXX-000000",
            BatchName = "UNITY_BATCH_1",
            BatchNumber = 1
        };
        _ = await _paymentRequestRepository.InsertAsync(new PaymentRequest(Guid.NewGuid(), paymentRequestDto), true);

        // Act
        var paymentRequests = await _paymentRequestAppService.GetListAsync(new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto()
        {
            MaxResultCount = 100
        });

        // Assert           
        paymentRequests.TotalCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_ReturnsPagedPaymentsList()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var supplier = new Supplier(Guid.NewGuid(), "supp", "123", new Modules.Shared.Correlation.Correlation(Guid.NewGuid(), "A"));
        supplier.AddSite(new Site(Guid.NewGuid(), "123", PaymentGroup.EFT));
        var addedSupplier = await _supplierRepository.InsertAsync(supplier);
        CreatePaymentRequestDto paymentRequestDto = new()
        {
            InvoiceNumber = "INV-001",
            Amount = 100,
            PayeeName = "Test Payee",
            ContractNumber = "0000000000",
            SupplierNumber = "SUP-001",
            SiteId = addedSupplier.Sites[0].Id,
            CorrelationId = Guid.NewGuid(),
            CorrelationProvider = "TestProvider",
            ReferenceNumber = "UP-XXXX-000001",
            BatchName = "UNITY_BATCH_1",
            BatchNumber = 1
        };
        _ = await _paymentRequestRepository.InsertAsync(new PaymentRequest(Guid.NewGuid(), paymentRequestDto), true);

        // Act
        var paymentRequests = await _paymentRequestAppService.GetListAsync(new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto
        {
            MaxResultCount = 10,
            SkipCount = 0,
            Sorting = "CreationTime desc"
        });

        // Assert
        paymentRequests.TotalCount.ShouldBeGreaterThan(0);
        paymentRequests.Items.ShouldNotBeEmpty();
        paymentRequests.Items[0].InvoiceNumber.ShouldBe("INV-001");
        paymentRequests.Items[0].CreatorId.ShouldBe(PaymentsTestData.UserDataMocks.User1.Id);
    }
}
