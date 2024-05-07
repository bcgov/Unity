using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;

using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Enums;
using Unity.Payments.Repositories;
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

        List<CreatePaymentRequestDto> paymentRequests = new List<CreatePaymentRequestDto> { new CreatePaymentRequestDto() {
             Amount =0,
               InvoiceNumber ="Test",
               ContractNumber ="",
               CorrelationId = Guid.NewGuid(),
               Description = "",
               PayeeName= "",
               SiteId= siteId,
               SupplierNumber = "",
        } };
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
        _ = await _paymentRequestRepository
            .InsertAsync(new PaymentRequest(Guid.NewGuid(),"",100,"Test","0000000000","", Guid.NewGuid(), Guid.NewGuid(), ""), true);

        // Act
        var batchPayments = await _paymentRequestAppService.GetListAsync(new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto()
        {
            MaxResultCount = 100
        });

        // Assert           
        batchPayments.TotalCount.ShouldBeGreaterThan(0);
    }
}
