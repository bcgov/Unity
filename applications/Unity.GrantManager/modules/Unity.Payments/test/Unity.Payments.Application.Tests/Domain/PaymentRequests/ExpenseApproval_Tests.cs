using Shouldly;
using System;
using System.ComponentModel;
using Unity.Payments.Enums;
using Xunit;

namespace Unity.Payments.Domain.PaymentRequests;

[Category("Domain")]
public class ExpenseApproval_Tests : PaymentsApplicationTestBase
{
    [Fact]
    public void Create_ValidParameters_ShouldNotThrow()
    {
        // Arrange
        var id = Guid.NewGuid();
        var type = ExpenseApprovalType.Level1;

        // Act
        var expenseApproval = new ExpenseApproval(id, type);

        // Assert
        expenseApproval.ShouldNotBeNull();
        expenseApproval.Type.ShouldBe(type);
        expenseApproval.Status.ShouldBe(ExpenseApprovalStatus.Requested);
    }

    [Fact]
    public void Approve_ShouldSetStatusAndDecisionFields()
    {
        // Arrange
        var id = Guid.NewGuid();
        var type = ExpenseApprovalType.Level1;
        var expenseApproval = new ExpenseApproval(id, type);
        var currentUserId = Guid.NewGuid();

        // Act
        expenseApproval.Approve(currentUserId);

        // Assert
        expenseApproval.Status.ShouldBe(ExpenseApprovalStatus.Approved);
        expenseApproval.DecisionUserId.ShouldBe(currentUserId);
        expenseApproval.DecisionDate.ShouldNotBeNull();
    }

    [Fact]
    public void Decline_ShouldSetStatusAndDecisionFields()
    {
        // Arrange
        var id = Guid.NewGuid();
        var type = ExpenseApprovalType.Level1;
        var expenseApproval = new ExpenseApproval(id, type);
        var currentUserId = Guid.NewGuid();

        // Act
        expenseApproval.Decline(currentUserId);

        // Assert
        expenseApproval.Status.ShouldBe(ExpenseApprovalStatus.Declined);
        expenseApproval.DecisionUserId.ShouldBe(currentUserId);
        expenseApproval.DecisionDate.ShouldNotBeNull();
    }

    [Fact]
    public void SettingDecisionUserIdTwice_ShouldThrow()
    {
        // Arrange
        var id = Guid.NewGuid();
        var type = ExpenseApprovalType.Level1;
        var expenseApproval = new ExpenseApproval(id, type);
        var currentUserId = Guid.NewGuid();

        // Act
        expenseApproval.Approve(currentUserId);

        // Assert
        Assert.Throws<InvalidOperationException>(() => expenseApproval.DecisionUserId = Guid.NewGuid());
    }

    [Fact]
    public void SettingDecisionDateTwice_ShouldThrow()
    {
        // Arrange
        var id = Guid.NewGuid();
        var type = ExpenseApprovalType.Level1;
        var expenseApproval = new ExpenseApproval(id, type);
        var currentUserId = Guid.NewGuid();

        // Act
        expenseApproval.Approve(currentUserId);

        // Assert
        Assert.Throws<InvalidOperationException>(() => expenseApproval.DecisionDate = DateTime.UtcNow);
    }

    [Fact]
    public void AccessingUninitializedPaymentRequest_ShouldThrow()
    {
        // Arrange
        var id = Guid.NewGuid();
        var type = ExpenseApprovalType.Level1;
        var expenseApproval = new ExpenseApproval(id, type);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => { var paymentRequest = expenseApproval.PaymentRequest; });
    }

    [Fact]
    public void SettingPaymentRequest_ShouldReturnSetValue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expenseApproval = new ExpenseApproval(id, ExpenseApprovalType.Level1);
        var fakePaymentRequest = new FakePaymentRequest();

        // Act
        expenseApproval.PaymentRequest = fakePaymentRequest;

        // Assert
        expenseApproval.PaymentRequest.ShouldBe(fakePaymentRequest);
    }

    private class FakePaymentRequest : PaymentRequest
    {
        public FakePaymentRequest() : base()
        {
            // No initialization required for testing getter and setter.
        }
    }
}
