using Microsoft.EntityFrameworkCore;
using Unity.Notifications.Emails;
using Unity.Notifications.Templates;
using Unity.Notifications.EmailGroups;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Notifications.EntityFrameworkCore;

[ConnectionStringName(NotificationsDbProperties.ConnectionStringName)]
public class NotificationsDbContext : AbpDbContext<NotificationsDbContext>, INotificationsDbContext
{
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<EmailLogAttachment> EmailLogAttachments { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    public DbSet<TemplateVariable> TemplateVariables { get; set; }
    public DbSet<EmailGroup> EmailGroups { get; set; }
    public DbSet<EmailGroupUser> EmailGroupUsers { get; set; }

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
