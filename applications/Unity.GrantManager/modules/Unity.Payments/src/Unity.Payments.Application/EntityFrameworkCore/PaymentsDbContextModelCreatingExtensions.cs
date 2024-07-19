using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Domain.PaymentConfigurations;

namespace Unity.Payments.EntityFrameworkCore;

public static class PaymentsDbContextModelCreatingExtensions
{
    public static void ConfigurePayments(
        this ModelBuilder modelBuilder)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));

        modelBuilder.Entity<PaymentRequest>(b =>
        {
            b.ToTable(PaymentsDbProperties.DbTablePrefix + "PaymentRequests",
                PaymentsDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.HasMany(e => e.ExpenseApprovals)
                .WithOne(e => e.PaymentRequest)
                .HasForeignKey(x => x.PaymentRequestId);

            b.HasOne(e => e.Site)
                .WithMany()
                .HasForeignKey(x => x.SiteId)
                .OnDelete(DeleteBehavior.NoAction);
          
            b.HasIndex(e => e.ReferenceNumber).IsUnique();
        });


        modelBuilder.Entity<ExpenseApproval>(b =>
        {
            b.ToTable(PaymentsDbProperties.DbTablePrefix + "ExpenseApprovals",
                PaymentsDbProperties.DbSchema);

            b.ConfigureByConvention();
        });

        modelBuilder.Entity<Supplier>(b =>
        {
            b.ToTable(PaymentsDbProperties.DbTablePrefix + "Suppliers",
                PaymentsDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.HasMany(e => e.Sites)
                .WithOne(e => e.Supplier)
                .HasForeignKey(x => x.SupplierId);
        });


        modelBuilder.Entity<Site>(b =>
        {
            b.ToTable(PaymentsDbProperties.DbTablePrefix + "Sites",
                PaymentsDbProperties.DbSchema);

            b.ConfigureByConvention();
        });

        modelBuilder.Entity<PaymentConfiguration>(b =>
        {
            b.ToTable(PaymentsDbProperties.DbTablePrefix + "PaymentConfigurations",
                PaymentsDbProperties.DbSchema);

            b.ConfigureByConvention();
        });
    }
}
