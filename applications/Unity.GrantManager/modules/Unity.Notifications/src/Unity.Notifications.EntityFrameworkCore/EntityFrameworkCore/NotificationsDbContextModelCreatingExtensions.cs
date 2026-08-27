using Microsoft.EntityFrameworkCore;

using Unity.Notifications.Emails;
using Unity.Notifications.Logs;
using Unity.Notifications.ReadStates;
using Unity.Notifications.Templates;
using Unity.Notifications.EmailGroups;
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

            b.Property(x => x.Recipient).HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.EmailType).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<EmailLogAttachment>(b =>
        {
            b.ToTable(NotificationsDbProperties.DbTablePrefix + "EmailLogAttachments",
                NotificationsDbProperties.DbSchema);

            b.ConfigureByConvention();

            // Foreign key to EmailLog with CASCADE delete
            b.HasOne<EmailLog>()
                .WithMany()
                .IsRequired(false)
                .HasForeignKey(x => x.EmailLogId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne<EmailTemplate>()
                .WithMany()
                .IsRequired(false)
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);                

            // Indexes for performance
            b.HasIndex(x => x.TemplateId);
            b.HasIndex(x => x.EmailLogId);
            b.HasIndex(x => x.S3ObjectKey);
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
        modelBuilder.Entity<EmailGroup>(b =>
        {
            b.ToTable(NotificationsDbProperties.DbTablePrefix + "EmailGroups", NotificationsDbProperties.DbSchema);

            b.ConfigureByConvention();
        });

        modelBuilder.Entity<EmailGroupUser>(b =>
        {
            b.ToTable(NotificationsDbProperties.DbTablePrefix + "EmailGroupUsers", NotificationsDbProperties.DbSchema);

            b.ConfigureByConvention();
            b.HasOne<EmailGroup>()
              .WithMany()
              .HasForeignKey(x => x.GroupId);
        });

        modelBuilder.Entity<NotificationLog>(b =>
        {
            b.ToTable(NotificationsDbProperties.DbTablePrefix + "NotificationLogs", NotificationsDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.Property(x => x.NotificationType).HasConversion<string>().HasMaxLength(64);
            b.Property(x => x.Channel).HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.Severity).HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Message).IsRequired();
            b.Property(x => x.Source).IsRequired().HasMaxLength(200);
            b.Property(x => x.SourceReference).HasMaxLength(256);
            b.Property(x => x.CorrelationId).HasMaxLength(128);
            b.Property(x => x.DeliveryTarget).HasMaxLength(256);
            b.Property(x => x.ExceptionType).HasMaxLength(256);
            b.Property(x => x.Environment).HasMaxLength(64);
            b.Property(x => x.CommitSha).HasMaxLength(64);
            b.Property(x => x.PayloadJson).HasColumnType("jsonb");
            b.Property(x => x.SenderDisplayName).HasMaxLength(256);

            b.HasIndex(x => new { x.TenantId, x.CreationTime });
            b.HasIndex(x => new { x.NotificationType, x.CreationTime });
            b.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.Entity<NotificationReadState>(b =>
        {
            b.ToTable(NotificationsDbProperties.DbTablePrefix + "NotificationReadStates", NotificationsDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
        });
    }
}
