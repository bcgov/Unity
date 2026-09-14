using Microsoft.EntityFrameworkCore;
using Unity.Notifications.Emails;
using Unity.Notifications.Logs;
using Unity.Notifications.ReadStates;
using Unity.Notifications.Templates;
using Unity.Notifications.EmailAddresses;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Notifications.EntityFrameworkCore;

[ConnectionStringName(NotificationsDbProperties.ConnectionStringName)]
public interface INotificationsDbContext : IEfCoreDbContext
{
    // Add DbSet for each Aggregate Root here.
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<EmailLogAttachment> EmailLogAttachments { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    public DbSet<TemplateVariable> TemplateVariables { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    public DbSet<NotificationReadState> NotificationReadStates { get; set; }
    public DbSet<EmailAddressConfiguration> EmailAddressConfigurations { get; set; }
}
