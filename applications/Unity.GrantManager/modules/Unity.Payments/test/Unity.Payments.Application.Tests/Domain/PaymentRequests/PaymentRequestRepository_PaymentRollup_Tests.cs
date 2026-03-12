using Shouldly;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Unity.Modules.Shared.Correlation;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Enums;
using Unity.Payments.PaymentRequests;
using Volo.Abp.Uow;
using Xunit;

namespace Unity.Payments.Domain.PaymentRequests;

[Category("Integration")]
public class PaymentRequestRepository_PaymentRollup_Tests : PaymentsApplicationTestBase
{
    private readonly IPaymentRequestRepository _paymentRequestRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public PaymentRequestRepository_PaymentRollup_Tests()
    {
        _paymentRequestRepository = GetRequiredService<IPaymentRequestRepository>();
        _supplierRepository = GetRequiredService<ISupplierRepository>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
    }

    #region PaymentStatus Case-Insensitive Matching

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Count_FullyPaid_With_Exact_Case()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m,
            PaymentRequestStatus.Submitted, paymentStatus: "Fully Paid");

        // Act
        var results = await _paymentRequestRepository
            .GetBatchPaymentRollupsByCorrelationIdsAsync([correlationId]);

        // Assert
        results.Count.ShouldBe(1);
        results[0].ApplicationId.ShouldBe(correlationId);
        results[0].TotalPaid.ShouldBe(1000m);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Count_FullyPaid_With_UpperCase()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 500m,
            PaymentRequestStatus.Submitted, paymentStatus: "FULLY PAID");

        // Act
        var results = await _paymentRequestRepository
            .GetBatchPaymentRollupsByCorrelationIdsAsync([correlationId]);

        // Assert
        results.Count.ShouldBe(1);
        results[0].TotalPaid.ShouldBe(500m);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Count_FullyPaid_With_LowerCase()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 800m,
            PaymentRequestStatus.Submitted, paymentStatus: "fully paid");

        // Act
        var results = await _paymentRequestRepository
            .GetBatchPaymentRollupsByCorrelationIdsAsync([correlationId]);

        // Assert
        results.Count.ShouldBe(1);
        results[0].TotalPaid.ShouldBe(800m);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Count_FullyPaid_With_LeadingAndTrailingSpaces()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 300m,
            PaymentRequestStatus.Submitted, paymentStatus: "  Fully Paid  ");

        // Act
        var results = await _paymentRequestRepository
            .GetBatchPaymentRollupsByCorrelationIdsAsync([correlationId]);

        // Assert
        results.Count.ShouldBe(1);
        results[0].TotalPaid.ShouldBe(300m);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Aggregate_FullyPaid_Across_All_Case_Variations()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m,
            PaymentRequestStatus.Submitted, paymentStatus: "Fully Paid");
        await InsertPaymentRequestAsync(siteId, correlationId, 500m,
            PaymentRequestStatus.Submitted, paymentStatus: "FULLY PAID");
        await InsertPaymentRequestAsync(siteId, correlationId, 800m,
            PaymentRequestStatus.Submitted, paymentStatus: "fully paid");
        await InsertPaymentRequestAsync(siteId, correlationId, 300m,
            PaymentRequestStatus.Submitted, paymentStatus: "  Fully Paid  ");

        // Act
        var results = await _paymentRequestRepository
            .GetBatchPaymentRollupsByCorrelationIdsAsync([correlationId]);

        // Assert
        results.Count.ShouldBe(1);
        results[0].TotalPaid.ShouldBe(2600m); // 1000 + 500 + 800 + 300
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Not_Count_PartialMatch_PaymentStatus_As_Paid()
    {
        // Arrange - "Paid" alone should not match "Fully Paid"
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 200m,
            PaymentRequestStatus.Submitted, paymentStatus: "Paid");

        // Act
        var results = await _paymentRequestRepository
            .GetBatchPaymentRollupsByCorrelationIdsAsync([correlationId]);

        // Assert
        results.Count.ShouldBe(1);
        results[0].TotalPaid.ShouldBe(0m); // "Paid" != "Fully Paid"
    }

    #endregion

    #region Pending Status Calculation

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Sum_All_Pending_Levels()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m,
            PaymentRequestStatus.L1Pending);
        await InsertPaymentRequestAsync(siteId, correlationId, 2000m,
            PaymentRequestStatus.L2Pending);
        await InsertPaymentRequestAsync(siteId, correlationId, 3000m,
            PaymentRequestStatus.L3Pending);

        // Act
        var results = await _paymentRequestRepository
            .GetBatchPaymentRollupsByCorrelationIdsAsync([correlationId]);

        // Assert
        results.Count.ShouldBe(1);
        results[0].TotalPending.ShouldBe(6000m); // 1000 + 2000 + 3000
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Include_Submitted_WithNullPaymentStatus_InPending()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 500m,
            PaymentRequestStatus.Submitted, paymentStatus: null, invoiceStatus: null);
        await InsertPaymentRequestAsync(siteId, correlationId, 300m,
            PaymentRequestStatus.Submitted, paymentStatus: null, invoiceStatus: "SentToCas");

        // Act
        var results = await _paymentRequestRepository
            .GetBatchPaymentRollupsByCorrelationIdsAsync([correlationId]);

        // Assert
        results.Count.ShouldBe(1);
        results[0].TotalPending.ShouldBe(800m); // 500 + 300
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Exclude_Submitted_WithNotFound_InvoiceStatus_FromPending()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        // NotFound invoice status - should NOT be counted as pending
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m,
            PaymentRequestStatus.Submitted, paymentStatus: null, invoiceStatus: "NotFound");
        // Valid pending - SHOULD be counted
        await InsertPaymentRequestAsync(siteId, correlationId, 200m,
            PaymentRequestStatus.Submitted, paymentStatus: null, invoiceStatus: "SentToCas");

        // Act
        var results = await _paymentRequestRepository
            .GetBatchPaymentRollupsByCorrelationIdsAsync([correlationId]);

        // Assert
        results.Count.ShouldBe(1);
        results[0].TotalPending.ShouldBe(200m); // Only the non-NotFound one
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Exclude_Submitted_WithErrorFromCas_FromPending()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        // This one has ErrorFromCas - should NOT be pending
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m,
            PaymentRequestStatus.Submitted, paymentStatus: null, invoiceStatus: "Error");
        // This one has no error - SHOULD be pending
        await InsertPaymentRequestAsync(siteId, correlationId, 200m,
            PaymentRequestStatus.Submitted, paymentStatus: null, invoiceStatus: "SentToCas");

        // Act
        var results = await _paymentRequestRepository
            .GetBatchPaymentRollupsByCorrelationIdsAsync([correlationId]);

        // Assert
        results.Count.ShouldBe(1);
        results[0].TotalPending.ShouldBe(200m); // Only the non-error one
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Exclude_Declined_Statuses_From_Both_Paid_And_Pending()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m,
            PaymentRequestStatus.L1Declined);
        await InsertPaymentRequestAsync(siteId, correlationId, 2000m,
            PaymentRequestStatus.L2Declined);
        await InsertPaymentRequestAsync(siteId, correlationId, 3000m,
            PaymentRequestStatus.L3Declined);

        // Act
        var results = await _paymentRequestRepository
            .GetBatchPaymentRollupsByCorrelationIdsAsync([correlationId]);

        // Assert
        results.Count.ShouldBe(1);
        results[0].TotalPaid.ShouldBe(0m);
        results[0].TotalPending.ShouldBe(0m);
    }

    #endregion

    #region Mixed Paid and Pending

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Correctly_Separate_Paid_And_Pending()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        // Paid
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m,
            PaymentRequestStatus.Submitted, paymentStatus: "Fully Paid");
        await InsertPaymentRequestAsync(siteId, correlationId, 500m,
            PaymentRequestStatus.Submitted, paymentStatus: "FULLY PAID");
        // Pending
        await InsertPaymentRequestAsync(siteId, correlationId, 2000m,
            PaymentRequestStatus.L1Pending);
        await InsertPaymentRequestAsync(siteId, correlationId, 800m,
            PaymentRequestStatus.Submitted, paymentStatus: null, invoiceStatus: null);
        // Neither paid nor pending (declined)
        await InsertPaymentRequestAsync(siteId, correlationId, 5000m,
            PaymentRequestStatus.L1Declined);

        // Act
        var results = await _paymentRequestRepository
            .GetBatchPaymentRollupsByCorrelationIdsAsync([correlationId]);

        // Assert
        results.Count.ShouldBe(1);
        results[0].TotalPaid.ShouldBe(1500m);   // 1000 + 500
        results[0].TotalPending.ShouldBe(2800m); // 2000 + 800
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_Rollup_For_Multiple_CorrelationIds()
    {
        // Arrange
        var app1Id = Guid.NewGuid();
        var app2Id = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        // App 1 payments
        await InsertPaymentRequestAsync(siteId, app1Id, 1000m,
            PaymentRequestStatus.Submitted, paymentStatus: "Fully Paid");
        await InsertPaymentRequestAsync(siteId, app1Id, 500m,
            PaymentRequestStatus.L1Pending);
        // App 2 payments
        await InsertPaymentRequestAsync(siteId, app2Id, 2000m,
            PaymentRequestStatus.Submitted, paymentStatus: "fully paid");
        await InsertPaymentRequestAsync(siteId, app2Id, 300m,
            PaymentRequestStatus.L2Pending);

        // Act
        var results = await _paymentRequestRepository
            .GetBatchPaymentRollupsByCorrelationIdsAsync([app1Id, app2Id]);

        // Assert
        results.Count.ShouldBe(2);

        var app1Rollup = results.Find(r => r.ApplicationId == app1Id);
        app1Rollup.ShouldNotBeNull();
        app1Rollup!.TotalPaid.ShouldBe(1000m);
        app1Rollup.TotalPending.ShouldBe(500m);

        var app2Rollup = results.Find(r => r.ApplicationId == app2Id);
        app2Rollup.ShouldNotBeNull();
        app2Rollup!.TotalPaid.ShouldBe(2000m);
        app2Rollup.TotalPending.ShouldBe(300m);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_Empty_For_Unknown_CorrelationIds()
    {
        // Arrange & Act
        using var uow = _unitOfWorkManager.Begin();
        var results = await _paymentRequestRepository
            .GetBatchPaymentRollupsByCorrelationIdsAsync([Guid.NewGuid()]);

        // Assert
        results.ShouldBeEmpty();
    }

    #endregion

    #region Helpers

    private async Task<Guid> CreateSupplierAndSiteAsync()
    {
        using var uow = _unitOfWorkManager.Begin();
        var siteId = Guid.NewGuid();
        var supplier = new Supplier(Guid.NewGuid(), "TestSupplier", "SUP-001",
            new Correlation(Guid.NewGuid(), "Test"));
        supplier.AddSite(new Site(siteId, "001", PaymentGroup.EFT));
        await _supplierRepository.InsertAsync(supplier, true);
        await uow.CompleteAsync();
        return siteId;
    }

    private async Task InsertPaymentRequestAsync(
        Guid siteId,
        Guid correlationId,
        decimal amount,
        PaymentRequestStatus status,
        string? paymentStatus = null,
        string? invoiceStatus = null)
    {
        var dto = new CreatePaymentRequestDto
        {
            InvoiceNumber = $"INV-{Guid.NewGuid():N}",
            Amount = amount,
            PayeeName = "Test Payee",
            ContractNumber = "0000000000",
            SupplierNumber = "SUP-001",
            SiteId = siteId,
            CorrelationId = correlationId,
            CorrelationProvider = "Test",
            ReferenceNumber = $"REF-{Guid.NewGuid():N}",
            BatchName = "TEST_BATCH",
            BatchNumber = 1
        };

        var paymentRequest = new PaymentRequest(Guid.NewGuid(), dto);
        paymentRequest.SetPaymentRequestStatus(status);

        if (paymentStatus != null)
        {
            paymentRequest.SetPaymentStatus(paymentStatus);
        }

        if (invoiceStatus != null)
        {
            paymentRequest.SetInvoiceStatus(invoiceStatus);
        }

        await _paymentRequestRepository.InsertAsync(paymentRequest, true);
    }

    #endregion
}
