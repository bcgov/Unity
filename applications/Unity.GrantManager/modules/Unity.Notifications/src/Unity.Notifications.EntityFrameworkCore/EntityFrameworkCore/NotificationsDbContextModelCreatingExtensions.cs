using Microsoft.EntityFrameworkCore;
using Unity.Notifications.Emails;
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
    }
}
