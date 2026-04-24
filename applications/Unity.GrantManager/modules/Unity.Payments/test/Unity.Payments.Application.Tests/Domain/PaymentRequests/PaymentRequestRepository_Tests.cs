using Shouldly;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.Modules.Shared.Correlation;
using Unity.Payments.Codes;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Enums;
using Unity.Payments.PaymentRequests;
using Volo.Abp.Uow;
using Xunit;

namespace Unity.Payments.Domain.PaymentRequests;

[Category("Integration")]
public class PaymentRequestRepository_Tests : PaymentsApplicationTestBase
{
    private readonly IPaymentRequestRepository _paymentRequestRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public PaymentRequestRepository_Tests()
    {
        _paymentRequestRepository = GetRequiredService<IPaymentRequestRepository>();
        _supplierRepository = GetRequiredService<ISupplierRepository>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
    }

    #region GetCountByCorrelationId

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetCountByCorrelationId_Should_Return_Zero_When_No_Payments_Exist()
    {
        // Arrange
        var correlationId = Guid.NewGuid();

        // Act
        var count = await _paymentRequestRepository.GetCountByCorrelationId(correlationId);

        // Assert
        count.ShouldBe(0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetCountByCorrelationId_Should_Return_Correct_Count_For_Single_Payment()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.L1Pending);

        // Act
        var count = await _paymentRequestRepository.GetCountByCorrelationId(correlationId);

        // Assert
        count.ShouldBe(1);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetCountByCorrelationId_Should_Return_Correct_Count_For_Multiple_Payments()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.L1Pending);
        await InsertPaymentRequestAsync(siteId, correlationId, 2000m, PaymentRequestStatus.L2Pending);
        await InsertPaymentRequestAsync(siteId, correlationId, 3000m, PaymentRequestStatus.Submitted);

        // Act
        var count = await _paymentRequestRepository.GetCountByCorrelationId(correlationId);

        // Assert
        count.ShouldBe(3);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetCountByCorrelationId_Should_Not_Count_Other_CorrelationIds()
    {
        // Arrange
        var correlationId1 = Guid.NewGuid();
        var correlationId2 = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId1, 1000m, PaymentRequestStatus.L1Pending);
        await InsertPaymentRequestAsync(siteId, correlationId2, 2000m, PaymentRequestStatus.L1Pending);

        // Act
        var count = await _paymentRequestRepository.GetCountByCorrelationId(correlationId1);

        // Assert
        count.ShouldBe(1);
    }

    #endregion

    #region GetPaymentRequestCountBySiteId

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestCountBySiteId_Should_Return_Zero_When_No_Payments_Exist()
    {
        // Arrange
        var siteId = await CreateSupplierAndSiteAsync();

        // Act
        var count = await _paymentRequestRepository.GetPaymentRequestCountBySiteId(siteId);

        // Assert
        count.ShouldBe(0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestCountBySiteId_Should_Return_Correct_Count_For_Single_Payment()
    {
        // Arrange
        var siteId = await CreateSupplierAndSiteAsync();
        var correlationId = Guid.NewGuid();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.L1Pending);

        // Act
        var count = await _paymentRequestRepository.GetPaymentRequestCountBySiteId(siteId);

        // Assert
        count.ShouldBe(1);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestCountBySiteId_Should_Return_Correct_Count_For_Multiple_Payments()
    {
        // Arrange
        var siteId = await CreateSupplierAndSiteAsync();
        var correlationId1 = Guid.NewGuid();
        var correlationId2 = Guid.NewGuid();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId1, 1000m, PaymentRequestStatus.L1Pending);
        await InsertPaymentRequestAsync(siteId, correlationId2, 2000m, PaymentRequestStatus.L2Pending);

        // Act
        var count = await _paymentRequestRepository.GetPaymentRequestCountBySiteId(siteId);

        // Assert
        count.ShouldBe(2);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestCountBySiteId_Should_Not_Count_Other_Sites()
    {
        // Arrange
        var siteId1 = await CreateSupplierAndSiteAsync();
        var siteId2 = await CreateSupplierAndSiteAsync();
        var correlationId = Guid.NewGuid();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId1, correlationId, 1000m, PaymentRequestStatus.L1Pending);
        await InsertPaymentRequestAsync(siteId2, correlationId, 2000m, PaymentRequestStatus.L1Pending);

        // Act
        var count = await _paymentRequestRepository.GetPaymentRequestCountBySiteId(siteId1);

        // Assert
        count.ShouldBe(1);
    }

    #endregion

    #region GetPaymentRequestByInvoiceNumber

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestByInvoiceNumber_Should_Return_Null_When_Not_Found()
    {
        // Arrange
        var invoiceNumber = "NONEXISTENT-INV-001";

        // Act
        var payment = await _paymentRequestRepository.GetPaymentRequestByInvoiceNumber(invoiceNumber);

        // Assert
        payment.ShouldBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestByInvoiceNumber_Should_Return_Payment_When_Found()
    {
        // Arrange
        var siteId = await CreateSupplierAndSiteAsync();
        var correlationId = Guid.NewGuid();
        var invoiceNumber = $"TEST-INV-{Guid.NewGuid():N}";

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.L1Pending, invoiceNumber);

        // Act
        var payment = await _paymentRequestRepository.GetPaymentRequestByInvoiceNumber(invoiceNumber);

        // Assert
        payment.ShouldNotBeNull();
        payment.InvoiceNumber.ShouldBe(invoiceNumber);
        payment.Amount.ShouldBe(1000m);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestByInvoiceNumber_Should_Return_Only_Matching_Invoice()
    {
        // Arrange
        var siteId = await CreateSupplierAndSiteAsync();
        var correlationId = Guid.NewGuid();
        var invoiceNumber1 = $"TEST-INV-{Guid.NewGuid():N}";
        var invoiceNumber2 = $"TEST-INV-{Guid.NewGuid():N}";

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.L1Pending, invoiceNumber1);
        await InsertPaymentRequestAsync(siteId, correlationId, 2000m, PaymentRequestStatus.L2Pending, invoiceNumber2);

        // Act
        var payment = await _paymentRequestRepository.GetPaymentRequestByInvoiceNumber(invoiceNumber1);

        // Assert
        payment.ShouldNotBeNull();
        payment.InvoiceNumber.ShouldBe(invoiceNumber1);
        payment.Amount.ShouldBe(1000m);
    }

    #endregion

    #region GetTotalPaymentRequestAmountByCorrelationIdAsync

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetTotalPaymentRequestAmountByCorrelationIdAsync_Should_Return_Zero_When_No_Payments()
    {
        // Arrange
        var correlationId = Guid.NewGuid();

        // Act
        var total = await _paymentRequestRepository.GetTotalPaymentRequestAmountByCorrelationIdAsync(correlationId);

        // Assert
        total.ShouldBe(0m);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetTotalPaymentRequestAmountByCorrelationIdAsync_Should_Sum_Single_Payment()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted);

        // Act
        var total = await _paymentRequestRepository.GetTotalPaymentRequestAmountByCorrelationIdAsync(correlationId);

        // Assert
        total.ShouldBe(1000m);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetTotalPaymentRequestAmountByCorrelationIdAsync_Should_Sum_Multiple_Payments()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted);
        await InsertPaymentRequestAsync(siteId, correlationId, 2500m, PaymentRequestStatus.Submitted);
        await InsertPaymentRequestAsync(siteId, correlationId, 3000m, PaymentRequestStatus.L1Pending);

        // Act
        var total = await _paymentRequestRepository.GetTotalPaymentRequestAmountByCorrelationIdAsync(correlationId);

        // Assert
        total.ShouldBe(6500m);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetTotalPaymentRequestAmountByCorrelationIdAsync_Should_Exclude_L1Declined()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted);
        await InsertPaymentRequestAsync(siteId, correlationId, 5000m, PaymentRequestStatus.L1Declined);

        // Act
        var total = await _paymentRequestRepository.GetTotalPaymentRequestAmountByCorrelationIdAsync(correlationId);

        // Assert
        total.ShouldBe(1000m); // Declined amount excluded
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetTotalPaymentRequestAmountByCorrelationIdAsync_Should_Exclude_L2Declined()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted);
        await InsertPaymentRequestAsync(siteId, correlationId, 3000m, PaymentRequestStatus.L2Declined);

        // Act
        var total = await _paymentRequestRepository.GetTotalPaymentRequestAmountByCorrelationIdAsync(correlationId);

        // Assert
        total.ShouldBe(1000m); // Declined amount excluded
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetTotalPaymentRequestAmountByCorrelationIdAsync_Should_Exclude_L3Declined()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted);
        await InsertPaymentRequestAsync(siteId, correlationId, 2000m, PaymentRequestStatus.L3Declined);

        // Act
        var total = await _paymentRequestRepository.GetTotalPaymentRequestAmountByCorrelationIdAsync(correlationId);

        // Assert
        total.ShouldBe(1000m); // Declined amount excluded
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetTotalPaymentRequestAmountByCorrelationIdAsync_Should_Exclude_NotFound_InvoiceStatus()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted);
        await InsertPaymentRequestAsync(siteId, correlationId, 4000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.NotFound);

        // Act
        var total = await _paymentRequestRepository.GetTotalPaymentRequestAmountByCorrelationIdAsync(correlationId);

        // Assert
        total.ShouldBe(1000m); // NotFound invoice excluded
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetTotalPaymentRequestAmountByCorrelationIdAsync_Should_Exclude_ErrorFromCas_InvoiceStatus()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted);
        await InsertPaymentRequestAsync(siteId, correlationId, 6000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.ErrorFromCas);

        // Act
        var total = await _paymentRequestRepository.GetTotalPaymentRequestAmountByCorrelationIdAsync(correlationId);

        // Assert
        total.ShouldBe(1000m); // ErrorFromCas invoice excluded
    }

    #endregion

    #region GetPaymentRequestsBySentToCasStatusAsync

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestsBySentToCasStatusAsync_Should_Return_Empty_When_No_Matching_Payments()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.Validated);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentRequestsBySentToCasStatusAsync();

        // Assert
        payments.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestsBySentToCasStatusAsync_Should_Return_ServiceUnavailable_Status()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.ServiceUnavailable);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentRequestsBySentToCasStatusAsync();

        // Assert
        payments.Count.ShouldBe(1);
        payments[0].InvoiceStatus.ShouldBe(CasPaymentRequestStatus.ServiceUnavailable);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestsBySentToCasStatusAsync_Should_Return_SentToCas_Status()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.SentToCas);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentRequestsBySentToCasStatusAsync();

        // Assert
        payments.Count.ShouldBe(1);
        payments[0].InvoiceStatus.ShouldBe(CasPaymentRequestStatus.SentToCas);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestsBySentToCasStatusAsync_Should_Return_NeverValidated_Status()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.NeverValidated);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentRequestsBySentToCasStatusAsync();

        // Assert
        payments.Count.ShouldBe(1);
        payments[0].InvoiceStatus.ShouldBe(CasPaymentRequestStatus.NeverValidated);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestsBySentToCasStatusAsync_Should_Return_All_ReCheck_Statuses()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.ServiceUnavailable);
        await InsertPaymentRequestAsync(siteId, correlationId, 2000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.SentToCas);
        await InsertPaymentRequestAsync(siteId, correlationId, 3000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.NeverValidated);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentRequestsBySentToCasStatusAsync();

        // Assert
        payments.Count.ShouldBe(3);
        payments.Any(p => p.InvoiceStatus == CasPaymentRequestStatus.ServiceUnavailable).ShouldBeTrue();
        payments.Any(p => p.InvoiceStatus == CasPaymentRequestStatus.SentToCas).ShouldBeTrue();
        payments.Any(p => p.InvoiceStatus == CasPaymentRequestStatus.NeverValidated).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestsBySentToCasStatusAsync_Should_Not_Return_Null_InvoiceStatus()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted,
            invoiceStatus: null);
        await InsertPaymentRequestAsync(siteId, correlationId, 2000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.SentToCas);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentRequestsBySentToCasStatusAsync();

        // Assert
        payments.Count.ShouldBe(1);
        payments[0].InvoiceStatus.ShouldBe(CasPaymentRequestStatus.SentToCas);
    }

    #endregion

    #region GetPaymentRequestsByFailedsStatusAsync

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestsByFailedsStatusAsync_Should_Return_Empty_When_No_Matching_Payments()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.Validated);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentRequestsByFailedsStatusAsync();

        // Assert
        payments.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestsByFailedsStatusAsync_Should_Return_ServiceUnavailable_Status()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        var paymentRequest = await InsertAndGetPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.ServiceUnavailable);

        // Update to trigger LastModificationTime
        paymentRequest.SetCasResponse("Test response");
        await _paymentRequestRepository.UpdateAsync(paymentRequest, true);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentRequestsByFailedsStatusAsync();

        // Assert
        payments.Count.ShouldBe(1);
        payments[0].InvoiceStatus.ShouldBe(CasPaymentRequestStatus.ServiceUnavailable);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestsByFailedsStatusAsync_Should_Return_ErrorFromCas_Status()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        var paymentRequest = await InsertAndGetPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.ErrorFromCas);

        // Update to trigger LastModificationTime
        paymentRequest.SetCasResponse("Test response");
        await _paymentRequestRepository.UpdateAsync(paymentRequest, true);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentRequestsByFailedsStatusAsync();

        // Assert
        payments.Count.ShouldBe(1);
        payments[0].InvoiceStatus.ShouldBe(CasPaymentRequestStatus.ErrorFromCas);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestsByFailedsStatusAsync_Should_Return_Both_Failed_Statuses()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        var paymentRequest1 = await InsertAndGetPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.ServiceUnavailable);
        var paymentRequest2 = await InsertAndGetPaymentRequestAsync(siteId, correlationId, 2000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.ErrorFromCas);

        // Update to trigger LastModificationTime
        paymentRequest1.SetCasResponse("Test response");
        await _paymentRequestRepository.UpdateAsync(paymentRequest1, true);
        paymentRequest2.SetCasResponse("Test response");
        await _paymentRequestRepository.UpdateAsync(paymentRequest2, true);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentRequestsByFailedsStatusAsync();

        // Assert
        payments.Count.ShouldBe(2);
        payments.Any(p => p.InvoiceStatus == CasPaymentRequestStatus.ServiceUnavailable).ShouldBeTrue();
        payments.Any(p => p.InvoiceStatus == CasPaymentRequestStatus.ErrorFromCas).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentRequestsByFailedsStatusAsync_Should_Not_Return_Null_InvoiceStatus()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        var paymentRequest1 = await InsertAndGetPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted,
            invoiceStatus: null);
        var paymentRequest2 = await InsertAndGetPaymentRequestAsync(siteId, correlationId, 2000m, PaymentRequestStatus.Submitted,
            invoiceStatus: CasPaymentRequestStatus.ErrorFromCas);

        // Update to trigger LastModificationTime (only update the one with ErrorFromCas)
        paymentRequest2.SetCasResponse("Test response");
        await _paymentRequestRepository.UpdateAsync(paymentRequest2, true);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentRequestsByFailedsStatusAsync();

        // Assert
        payments.Count.ShouldBe(1);
        payments[0].InvoiceStatus.ShouldBe(CasPaymentRequestStatus.ErrorFromCas);
    }

    #endregion

    #region GetPaymentPendingListByCorrelationIdAsync

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentPendingListByCorrelationIdAsync_Should_Return_Empty_When_No_Pending()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.Submitted);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentPendingListByCorrelationIdAsync(correlationId);

        // Assert
        payments.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentPendingListByCorrelationIdAsync_Should_Return_L1Pending()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.L1Pending);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentPendingListByCorrelationIdAsync(correlationId);

        // Assert
        payments.Count.ShouldBe(1);
        payments[0].Status.ShouldBe(PaymentRequestStatus.L1Pending);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentPendingListByCorrelationIdAsync_Should_Return_L2Pending()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.L2Pending);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentPendingListByCorrelationIdAsync(correlationId);

        // Assert
        payments.Count.ShouldBe(1);
        payments[0].Status.ShouldBe(PaymentRequestStatus.L2Pending);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentPendingListByCorrelationIdAsync_Should_Return_All_Pending_Statuses()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.L1Pending);
        await InsertPaymentRequestAsync(siteId, correlationId, 2000m, PaymentRequestStatus.L2Pending);
        await InsertPaymentRequestAsync(siteId, correlationId, 3000m, PaymentRequestStatus.Submitted);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentPendingListByCorrelationIdAsync(correlationId);

        // Assert
        payments.Count.ShouldBe(2);
        payments.Any(p => p.Status == PaymentRequestStatus.L1Pending).ShouldBeTrue();
        payments.Any(p => p.Status == PaymentRequestStatus.L2Pending).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentPendingListByCorrelationIdAsync_Should_Return_L3Pending()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId, 1000m, PaymentRequestStatus.L3Pending);        

        // Act
        var payments = await _paymentRequestRepository.GetPaymentPendingListByCorrelationIdAsync(correlationId);

        // Assert
        payments.Count.ShouldBe(1);
        payments[0].Status.ShouldBe(PaymentRequestStatus.L3Pending);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaymentPendingListByCorrelationIdAsync_Should_Only_Return_Matching_CorrelationId()
    {
        // Arrange
        var correlationId1 = Guid.NewGuid();
        var correlationId2 = Guid.NewGuid();
        var siteId = await CreateSupplierAndSiteAsync();

        using var uow = _unitOfWorkManager.Begin();
        await InsertPaymentRequestAsync(siteId, correlationId1, 1000m, PaymentRequestStatus.L1Pending);
        await InsertPaymentRequestAsync(siteId, correlationId2, 2000m, PaymentRequestStatus.L1Pending);

        // Act
        var payments = await _paymentRequestRepository.GetPaymentPendingListByCorrelationIdAsync(correlationId1);

        // Assert
        payments.Count.ShouldBe(1);
        payments[0].CorrelationId.ShouldBe(correlationId1);
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
        string? customInvoiceNumber = null,
        string? paymentStatus = null,
        string? invoiceStatus = null)
    {
        await InsertAndGetPaymentRequestAsync(siteId, correlationId, amount, status,
            customInvoiceNumber, paymentStatus, invoiceStatus);
    }

    private async Task<PaymentRequest> InsertAndGetPaymentRequestAsync(
        Guid siteId,
        Guid correlationId,
        decimal amount,
        PaymentRequestStatus status,
        string? customInvoiceNumber = null,
        string? paymentStatus = null,
        string? invoiceStatus = null)
    {
        var invoiceNumber = customInvoiceNumber ?? $"INV-{Guid.NewGuid():N}";
        var dto = new CreatePaymentRequestDto
        {
            InvoiceNumber = invoiceNumber,
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
        return paymentRequest;
    }

    #endregion
}
