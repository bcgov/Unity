using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Integrations;
using Unity.GrantManager.Notifications.Logs;
using Unity.Notifications.Teams;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Notifications
{
    // This class is responsible for first lookup up the Teams channel URL from the database and then posting notifications to the Teams Service.
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(NotificationsAppService), typeof(INotificationsAppService))]
    public class NotificationsAppService(IDynamicUrlRepository dynamicUrlRepository,
        ILogger<NotificationsAppService> logger, ICurrentTenant currentTenant) : INotificationsAppService, ITransientDependency
    {

        [UnitOfWork]
        public async Task<string> InitializeTeamsChannelAsync(string keyName)
        {
            using (currentTenant.Change(null))
            {
                DynamicUrl? teamsChannel = await dynamicUrlRepository.FirstOrDefaultAsync(q => q.KeyName == keyName && q.TenantId == null);
                if (teamsChannel?.Url == null)
                {
                    logger.LogWarning("Teams channel not found for key {KeyName}", keyName);
                    return string.Empty;
                }

                return teamsChannel.Url;
            }
        }

        [RemoteService(false)]
        public async Task NotifyChefsEventToTeamsAsync(string factName, string factValue, bool alert = false)
        {
            string? envInfo = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string activityTitle = "Chefs Submission Event Validation Error";
            string activitySubtitle = "Environment: " + envInfo;
            LogNotificationService LogNotificationService = new();
            LogNotificationService.AddFact(factName, factValue);
            await LogNotificationService.LogFactsToNotificationsAsync(NotificationType.UnityAlert, activityTitle, activitySubtitle);
        }

        [UnitOfWork]
        public async Task PostToNotificationsAsync(string activityTitle, string activitySubtitle, List<Fact> facts)
        {
            string teamsChannel = await InitializeTeamsChannelAsync(LogNotificationService.TEAMS_NOTIFICATION);
            if (teamsChannel.IsNullOrEmpty())
            {
                logger.LogWarning("PostToNotificationsAsync: no Teams channel configured, skipping notification LogNotificationService.TEAMS_NOTIFICATION");
                return;
            }

            string messageCard = LogNotificationService.InitializeMessageCard(activityTitle, activitySubtitle, facts);
            try
            {
                await LogNotificationService.PostToNotificationsChannelAsync(NotificationType.UnityAlert, messageCard);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to post Teams notification to channel");
            }
        }

        public async Task PostToNotificationsAsync(string activityTitle, string activitySubtitle)
        {
            string teamsChannel = await InitializeTeamsChannelAsync(LogNotificationService.TEAMS_NOTIFICATION);
            if (teamsChannel.IsNullOrEmpty())
            {
                logger.LogWarning("PostToNotificationsAsync (no-facts): no Teams channel configured, skipping notification");
                return;
            }
            List<Fact> facts = [];
            string messageCard = LogNotificationService.InitializeMessageCard(activityTitle, activitySubtitle, facts);
            try
            {
                await LogNotificationService.PostToNotificationsChannelAsync(NotificationType.UnityAlert, messageCard);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to post Teams notification to channel");
            }
        }

        public async Task PostChefsEventToTeamsAsync(string subscriptionEvent, dynamic form, dynamic chefsFormVersion)
        {
            await LogNotificationService.PostChefsEventToNotificationsAsync(NotificationType.ChefsEvent, subscriptionEvent, form, chefsFormVersion);
        }
    }
}