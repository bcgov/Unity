using Microsoft.EntityFrameworkCore;
using Unity.Payments.Domain;
using Unity.Payments.Domain.AccountCodings;
using Unity.Payments.Domain.PaymentConfigurations;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.PaymentTags;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Domain.PaymentThresholds;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.EntityFrameworkCore;

[ConnectionStringName(PaymentsDbProperties.ConnectionStringName)]
public class PaymentsDbContext : AbpDbContext<PaymentsDbContext>, IPaymentsDbContext
{
    public DbSet<PaymentRequest> PaymentRequests { get; set; }
    public DbSet<AccountCoding> AccountCoding { get; set; }
    public DbSet<ExpenseApproval> ExpenseApproval { get; set; }
    public DbSet<Supplier> Suppliers { get;set; }
    public DbSet<PaymentConfiguration> PaymentConfigurations { get;set; }
    public DbSet<PaymentThreshold> PaymentThresholds { get; set; }        
    public DbSet<Site> Sites { get; set; }
    public DbSet<PaymentTag> PaymentTags { get; set; }

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
