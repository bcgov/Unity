using Microsoft.EntityFrameworkCore;
using Unity.Payments.BatchPaymentRequests;
using Unity.Payments.PaymentSettings;
using Unity.Payments.Suppliers;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.EntityFrameworkCore;

[ConnectionStringName(PaymentsDbProperties.ConnectionStringName)]
public interface IPaymentsDbContext : IEfCoreDbContext
{
    public DbSet<BatchPaymentRequest> BatchPaymentRequests { get; }
    public DbSet<PaymentRequest> PaymentRequests { get;  }
    public DbSet<ExpenseApproval> ExpenseApproval { get;  }
    public DbSet<Supplier> Suppliers { get;  }
    public DbSet<Site> Sites { get; }
    public DbSet<PaymentSetting> PaymentSettings { get; }
}
