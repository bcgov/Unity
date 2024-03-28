using Microsoft.EntityFrameworkCore;
using Volo.Abp;

namespace Unity.Notifications.EntityFrameworkCore;

public static class NotificationsDbContextModelCreatingExtensions
{
    public static void ConfigureNotifications(
        this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));
        // Configure all entities here
    }
}
