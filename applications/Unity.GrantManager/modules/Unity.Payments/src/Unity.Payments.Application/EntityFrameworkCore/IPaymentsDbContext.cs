using Microsoft.EntityFrameworkCore;
using Unity.Payments.Domain;
using Unity.Payments.Domain.AccountCodings;
using Unity.Payments.Domain.PaymentConfigurations;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Domain.PaymentThresholds;
using Unity.Payments.Domain.PaymentTags;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.EntityFrameworkCore;

[ConnectionStringName(PaymentsDbProperties.ConnectionStringName)]
public interface IPaymentsDbContext : IEfCoreDbContext
{
    public DbSet<AccountCoding> AccountCoding { get; }
    public DbSet<PaymentRequest> PaymentRequests { get;  }
    public DbSet<ExpenseApproval> ExpenseApproval { get;  }
    public DbSet<Supplier> Suppliers { get;  }
    public DbSet<Site> Sites { get; }
    public DbSet<PaymentConfiguration> PaymentConfigurations { get; }
    public DbSet<PaymentThreshold> PaymentThresholds { get; }
    public DbSet<PaymentTag> PaymentTags { get; }
}
