using System;
using System.Threading.Tasks;
using Shouldly;

using Unity.Payments.Domain.BatchPaymentRequests;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Enums;
using Volo.Abp.Uow;
using Xunit;

namespace Unity.Payments.BatchPaymentRequests;

public class BatchPaymentRequestAppService_Tests : PaymentsApplicationTestBase
{
    private readonly IBatchPaymentRequestAppService _batchPaymentRequestAppService;
    private readonly IBatchPaymentRequestRepository _batchPaymentRequestRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public BatchPaymentRequestAppService_Tests()
    {
        _batchPaymentRequestAppService = GetRequiredService<IBatchPaymentRequestAppService>();
        _batchPaymentRequestRepository = GetRequiredService<IBatchPaymentRequestRepository>();
        _supplierRepository = GetRequiredService<ISupplierRepository>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
    }

    [Fact]
    [Trait("Category", "Integration")]    
    public async Task CreateAsync_CreatesBatchPaymentRequest()
    {
        // Arrange        
        using var uow = _unitOfWorkManager.Begin();
        var siteId = Guid.NewGuid();
        var newSupplier = new Supplier(Guid.NewGuid(),
            "Supplier",
            "123",
            Guid.NewGuid(),
            "Test",
            "Address1",
            "City",
            "Province",
            "ABC123");

        newSupplier.AddSite(new Site(siteId,
            "123",
            PaymentGroup.EFT,
            "123",
            "456",
            "789",
            "City",
            "Province",
            "ABC123"));

        _ = await _supplierRepository.InsertAsync(newSupplier, true);


        // Act        
        var insertedBatchPaymentRequest = await _batchPaymentRequestAppService
            .CreateAsync(new CreateBatchPaymentRequestDto()
            {
                Description = "Description",
                Provider = "A",
                PaymentRequests = [
                    new CreatePaymentRequestDto()
                    {
                        Amount = 1000,
                        CorrelationId = Guid.NewGuid(),
                        InvoiceNumber = "123",
                        Description = "123",
                        SiteId = siteId,
                    }
                ]
            });

        // Assert
        var batchPaymentRequest = await _batchPaymentRequestRepository.GetAsync(insertedBatchPaymentRequest.Id, true);
        batchPaymentRequest.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_ReturnsBatchPaymentsList()
    {
        // Arrange       
        _ = await _batchPaymentRequestRepository
            .InsertAsync(new BatchPaymentRequest(Guid.NewGuid(), "123", "description", "Bob", "Test"), true);

        // Act
        var batchPayments = await _batchPaymentRequestAppService.GetListAsync(new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto()
        {
            MaxResultCount = 100
        });

        // Assert           
        batchPayments.TotalCount.ShouldBeGreaterThan(0);
    }
}
