using Microsoft.EntityFrameworkCore;
using Unity.Payments.Domain;
using Unity.Payments.Domain.PaymentConfigurations;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.Suppliers;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.EntityFrameworkCore;

[ConnectionStringName(PaymentsDbProperties.ConnectionStringName)]
public class PaymentsDbContext : AbpDbContext<PaymentsDbContext>, IPaymentsDbContext
{   

    public DbSet<PaymentRequest> PaymentRequests { get; set; }
    public DbSet<ExpenseApproval> ExpenseApproval { get; set; }
    public DbSet<Supplier> Suppliers { get;set; }
    public DbSet<PaymentConfiguration> PaymentConfigurations { get;set; }
    public DbSet<Site> Sites { get; set; }

    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigurePayments();
    }
}
