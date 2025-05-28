using Microsoft.EntityFrameworkCore;
using Unity.Notifications.Emails;
using Unity.Notifications.Templates;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Unity.Notifications.EntityFrameworkCore;

public static class NotificationsDbContextModelCreatingExtensions
{
    public static void ConfigureNotifications(
        this ModelBuilder modelBuilder)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));
        modelBuilder.Entity<EmailLog>(b =>
        {
            b.ToTable(NotificationsDbProperties.DbTablePrefix + "EmailLogs",
                NotificationsDbProperties.DbSchema);

            b.ConfigureByConvention();
        });

        modelBuilder.Entity<EmailTemplate>(b =>
        {
            b.ToTable(NotificationsDbProperties.DbTablePrefix + "EmailTemplates",
                NotificationsDbProperties.DbSchema);

            b.ConfigureByConvention();

        });
        modelBuilder.Entity<Subscriber>(b =>
        {
            b.ToTable(NotificationsDbProperties.DbTablePrefix + "Subscribers",
                NotificationsDbProperties.DbSchema);

            b.ConfigureByConvention();
        });
        modelBuilder.Entity<SubscriptionGroup>(b =>
        {
            b.ToTable(NotificationsDbProperties.DbTablePrefix + "SubscriptionGroups",
                NotificationsDbProperties.DbSchema);

            b.ConfigureByConvention();
        });
        modelBuilder.Entity<SubscriptionGroupSubscription>(b =>
        {
            b.ToTable(NotificationsDbProperties.DbTablePrefix + "SubscriptionGroupSubscribers",
                NotificationsDbProperties.DbSchema);

            b.HasOne(ts => ts.Subscriber)
             .WithMany()
             .HasForeignKey(ts => ts.SubscriberId);

            b.HasOne(ts => ts.SubscriptionGroup)
                .WithMany()
                .HasForeignKey(ts => ts.GroupId);

            b.ConfigureByConvention();
        });
        modelBuilder.Entity<TemplateVariable>(b =>
        {
            b.ToTable(NotificationsDbProperties.DbTablePrefix + "TemplateVariables",
                NotificationsDbProperties.DbSchema);

            b.ConfigureByConvention();
        });
        modelBuilder.Entity<Trigger>(b =>
        {
            b.ToTable(NotificationsDbProperties.DbTablePrefix + "Triggers",
                NotificationsDbProperties.DbSchema);

            b.ConfigureByConvention();
        });
        modelBuilder.Entity<TriggerSubscription>(b =>
        {
            b.ToTable(NotificationsDbProperties.DbTablePrefix + "TriggerSubscriptions",
                NotificationsDbProperties.DbSchema);

            b.ConfigureByConvention();
            b.HasOne(ts => ts.Trigger)
               .WithMany() 
               .HasForeignKey(ts => ts.TriggerId);

            b.HasOne(ts => ts.EmailTemplate)
                .WithMany()
                .HasForeignKey(ts => ts.TemplateId);

            b.HasOne(ts => ts.SubscriptionGroup)
               .WithMany() 
               .HasForeignKey(ts => ts.SubscriptionGroupId);
        });
        modelBuilder.Entity<TemplateVariable>(b =>
        {
            b.ToTable(NotificationsDbProperties.DbTablePrefix + "TemplateVariables",
                NotificationsDbProperties.DbSchema);

            b.ConfigureByConvention();
        });
    }
}
