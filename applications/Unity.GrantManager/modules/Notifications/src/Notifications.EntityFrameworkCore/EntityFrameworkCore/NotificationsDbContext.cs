using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Notifications.EntityFrameworkCore;

[ConnectionStringName(NotificationsDbProperties.ConnectionStringName)]
public class NotificationsDbContext : AbpDbContext<NotificationsDbContext>, INotificationsDbContext
{
    /* Add DbSet for each Aggregate Root here. Example:
     * public DbSet<Question> Questions { get; set; }
     */

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureNotifications();
    }
}
