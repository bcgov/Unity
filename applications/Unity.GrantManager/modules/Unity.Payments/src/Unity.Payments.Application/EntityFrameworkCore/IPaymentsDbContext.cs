using Microsoft.EntityFrameworkCore;
using Unity.Payments.Domain;
using Unity.Payments.Domain.PaymentConfigurations;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.Suppliers;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.EntityFrameworkCore;

[ConnectionStringName(PaymentsDbProperties.ConnectionStringName)]
public interface IPaymentsDbContext : IEfCoreDbContext
{

    public DbSet<PaymentRequest> PaymentRequests { get;  }
    public DbSet<ExpenseApproval> ExpenseApproval { get;  }
    public DbSet<Supplier> Suppliers { get;  }
    public DbSet<Site> Sites { get; }
    public DbSet<PaymentConfiguration> PaymentConfigurations { get; }
}
