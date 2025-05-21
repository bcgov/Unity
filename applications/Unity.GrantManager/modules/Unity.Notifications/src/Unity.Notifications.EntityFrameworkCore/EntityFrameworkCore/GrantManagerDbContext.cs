using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Unity.GrantManager.Notifications.Settings;

namespace Unity.Notifications.EntityFrameworkCore;

[ConnectionStringName("Default")]
public class GrantManagerDbContext : AbpDbContext<NotificationsDbContext>
{
    public DbSet<DynamicUrl> DynamicUrls { get; set; }

    // Add DbSet for each Aggregate Root here.
    public GrantManagerDbContext(DbContextOptions<NotificationsDbContext> options)
        : base(options)
    {

    }
}
