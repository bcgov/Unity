﻿using Microsoft.EntityFrameworkCore;
using Unity.Payments.BatchPaymentRequests;
using Unity.Payments.Suppliers;
using Unity.Payments.PaymentSettings;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Unity.Payments.EntityFrameworkCore;

public static class PaymentsDbContextModelCreatingExtensions
{
    public static void ConfigurePayments(
        this ModelBuilder modelBuilder)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));

        modelBuilder.Entity<BatchPaymentRequest>(b =>
        {
            b.ToTable(PaymentsDbProperties.DbTablePrefix + "BatchPaymentRequests",
                PaymentsDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.HasMany(e => e.PaymentRequests)
                .WithOne(e => e.BatchPaymentRequest)
                .HasForeignKey(x => x.BatchPaymentRequestId);
            b.HasMany(e => e.ExpenseApprovals)
                .WithOne(e => e.BatchPaymentRequest)
                .HasForeignKey(x => x.BatchPaymentRequestId);
        });

        modelBuilder.Entity<PaymentRequest>(b =>
        {
            b.ToTable(PaymentsDbProperties.DbTablePrefix + "PaymentRequests",
                PaymentsDbProperties.DbSchema);

            b.ConfigureByConvention();
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

        modelBuilder.Entity<PaymentSetting>(b =>
        {
            b.ToTable(PaymentsDbProperties.DbTablePrefix + "PaymentSettings",
                PaymentsDbProperties.DbSchema);

            b.ConfigureByConvention();
        });
    }
}