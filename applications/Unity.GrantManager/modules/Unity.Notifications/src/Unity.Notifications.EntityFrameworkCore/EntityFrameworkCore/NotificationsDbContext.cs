using Microsoft.EntityFrameworkCore;
using Unity.Notifications.Emails;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Notifications.EntityFrameworkCore;

[ConnectionStringName(NotificationsDbProperties.ConnectionStringName)]
public class NotificationsDbContext : AbpDbContext<NotificationsDbContext>, INotificationsDbContext
{
    public DbSet<EmailLog> EmailLogs { get; set; }

    // Add DbSet for each Aggregate Root here.
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureNotifications();
    }
}
