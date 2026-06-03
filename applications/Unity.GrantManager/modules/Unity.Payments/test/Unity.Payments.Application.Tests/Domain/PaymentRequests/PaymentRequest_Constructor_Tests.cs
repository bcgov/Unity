using Shouldly;
using System;
using System.ComponentModel;
using Unity.Payments.Codes;
using Unity.Payments.Domain.Exceptions;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Enums;
using Unity.Payments.PaymentRequests;
using Volo.Abp;
using Xunit;

namespace Unity.Payments.Domain.PaymentRequests;

[Category("Domain")]
public class PaymentRequest_Constructor_Tests : PaymentsApplicationTestBase
{
    #region Normal payment constructor — new validations

    [Fact]
    public void NormalConstructor_WithEmptySiteId_ThrowsMissingSite()
    {
        var dto = BuildNormalDto(siteId: Guid.Empty);
        Should.Throw<BusinessException>(() => new PaymentRequest(Guid.NewGuid(), dto))
            .Code.ShouldBe(ErrorConsts.MissingSite);
    }

    [Fact]
    public void NormalConstructor_WithNullAccountCodingId_ThrowsMissingAccountCoding()
    {
        var dto = BuildNormalDto();
        dto.AccountCodingId = null;
        Should.Throw<BusinessException>(() => new PaymentRequest(Guid.NewGuid(), dto))
            .Code.ShouldBe(ErrorConsts.MissingAccountCoding);
    }

    [Fact]
    public void NormalConstructor_WithEmptyAccountCodingId_ThrowsMissingAccountCoding()
    {
        var dto = BuildNormalDto(accountCodingId: Guid.Empty);
        Should.Throw<BusinessException>(() => new PaymentRequest(Guid.NewGuid(), dto))
            .Code.ShouldBe(ErrorConsts.MissingAccountCoding);
    }

    [Fact]
    public void NormalConstructor_WithValidData_Succeeds()
    {
        var dto = BuildNormalDto();
        var payment = new PaymentRequest(Guid.NewGuid(), dto);
        payment.ShouldNotBeNull();
        payment.Status.ShouldBe(PaymentRequestStatus.L1Pending);
        payment.ExpenseApprovals.Count.ShouldBe(2);
    }

    #endregion

    #region Historical payment constructor

    [Fact]
    public void HistoricalConstructor_WithZeroAmount_ThrowsZeroPayment()
    {
        var dto = BuildHistoricalDto(amount: 0m);
        Should.Throw<BusinessException>(() => new PaymentRequest(Guid.NewGuid(), dto))
            .Code.ShouldBe(ErrorConsts.ZeroPayment);
    }

    [Fact]
    public void HistoricalConstructor_WithNegativeAmount_ThrowsZeroPayment()
    {
        var dto = BuildHistoricalDto(amount: -1m);
        Should.Throw<BusinessException>(() => new PaymentRequest(Guid.NewGuid(), dto))
            .Code.ShouldBe(ErrorConsts.ZeroPayment);
    }

    [Fact]
    public void HistoricalConstructor_WithNullSiteSupplierAndAccountCoding_DoesNotThrow()
    {
        var dto = BuildHistoricalDto();
        dto.SiteId.ShouldBeNull();
        dto.SupplierNumber.ShouldBeNull();
        dto.AccountCodingId.ShouldBeNull();

        Should.NotThrow(() => new PaymentRequest(Guid.NewGuid(), dto));
    }

    [Fact]
    public void HistoricalConstructor_SetsStatus_ToHistoricalPayment()
    {
        var payment = new PaymentRequest(Guid.NewGuid(), BuildHistoricalDto());
        payment.Status.ShouldBe(PaymentRequestStatus.HistoricalPayment);
    }

    [Fact]
    public void HistoricalConstructor_SetsPaymentStatus_ToPaid()
    {
        var payment = new PaymentRequest(Guid.NewGuid(), BuildHistoricalDto());
        payment.PaymentStatus.ShouldBe(CasPaymentRequestStatus.Paid);
    }

    [Fact]
    public void HistoricalConstructor_SetsInvoiceStatus_ToPaid()
    {
        var payment = new PaymentRequest(Guid.NewGuid(), BuildHistoricalDto());
        payment.InvoiceStatus.ShouldBe(CasPaymentRequestStatus.Paid);
    }

    [Fact]
    public void HistoricalConstructor_SetsPaymentDate_FromPaidDate()
    {
        var dto = BuildHistoricalDto();
        dto.PaidDate = "2025-06-15";

        var payment = new PaymentRequest(Guid.NewGuid(), dto);

        payment.PaymentDate.ShouldBe("2025-06-15");
    }

    [Fact]
    public void HistoricalConstructor_CreatesNoExpenseApprovals()
    {
        var payment = new PaymentRequest(Guid.NewGuid(), BuildHistoricalDto());
        payment.ExpenseApprovals.ShouldBeEmpty();
    }

    [Fact]
    public void HistoricalConstructor_WithOptionalFieldsProvided_StoresThem()
    {
        var siteId = Guid.NewGuid();
        var accountCodingId = Guid.NewGuid();
        var dto = BuildHistoricalDto();
        dto.SiteId = siteId;
        dto.SupplierNumber = "SUP-001";
        dto.SupplierName = "Test Supplier";
        dto.AccountCodingId = accountCodingId;

        var payment = new PaymentRequest(Guid.NewGuid(), dto);

        payment.SiteId.ShouldBe(siteId);
        payment.SupplierNumber.ShouldBe("SUP-001");
        payment.SupplierName.ShouldBe("Test Supplier");
        payment.AccountCodingId.ShouldBe(accountCodingId);
    }

    #endregion

    #region Helpers

    private static CreatePaymentRequestDto BuildNormalDto(
        Guid? siteId = null,
        Guid? accountCodingId = null) => new()
    {
        InvoiceNumber = "INV-001",
        Amount = 500m,
        PayeeName = "Test Payee",
        ContractNumber = "C-001",
        SupplierNumber = "SUP-001",
        SiteId = siteId ?? Guid.NewGuid(),
        CorrelationId = Guid.NewGuid(),
        CorrelationProvider = "Application",
        ReferenceNumber = $"REF-{Guid.NewGuid():N}",
        BatchName = "TEST_BATCH",
        BatchNumber = 1,
        AccountCodingId = accountCodingId ?? Guid.NewGuid()
    };

    private static CreateHistoricalPaymentRequestDto BuildHistoricalDto(decimal amount = 500m) => new()
    {
        InvoiceNumber = "HIST-INV-001",
        Amount = amount,
        PayeeName = "Test Payee",
        ContractNumber = "C-001",
        PaidDate = "2025-01-15",
        CorrelationId = Guid.NewGuid(),
        CorrelationProvider = "Application",
        ReferenceNumber = $"REF-{Guid.NewGuid():N}",
        BatchName = "HIST_BATCH",
        BatchNumber = 1
        // SiteId, SupplierNumber, SupplierName, AccountCodingId intentionally omitted
    };

    #endregion
}
