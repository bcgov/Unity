using Microsoft.EntityFrameworkCore;
using Unity.Payments.BatchPaymentRequests;
using Unity.Payments.PaymentSettings;
using Unity.Payments.Suppliers;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.EntityFrameworkCore;

[ConnectionStringName(PaymentsDbProperties.ConnectionStringName)]
public class PaymentsDbContext : AbpDbContext<PaymentsDbContext>, IPaymentsDbContext
{   
    public DbSet<BatchPaymentRequest> BatchPaymentRequests { get; set; }
    public DbSet<PaymentRequest> PaymentRequests { get; set; }
    public DbSet<ExpenseApproval> ExpenseApproval { get; set; }
    public DbSet<Supplier> Suppliers { get;set; }
    public DbSet<PaymentSetting> PaymentSettings { get;set; }
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
