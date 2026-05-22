using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Integrations;
using Unity.Notifications.TeamsNotifications;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Notifications
{
    // This class is responsible for first lookup up the Teams channel URL from the database and then posting notifications to the Teams Service.
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(NotificationsAppService), typeof(INotificationsAppService))]
    public class NotificationsAppService : INotificationsAppService, ITransientDependency
    {
        private readonly IDynamicUrlRepository _dynamicUrlRepository;
        private readonly TeamsNotificationService _teamsNotificationService;
        private readonly ILogger<NotificationsAppService> _logger;

        public NotificationsAppService(IDynamicUrlRepository dynamicUrlRepository, ILogger<NotificationsAppService> logger)
        {
            _dynamicUrlRepository = dynamicUrlRepository;
            _teamsNotificationService = new TeamsNotificationService();
            _logger = logger;
        }

        public async Task<string> InitializeTeamsChannelAsync(string keyName)
        {
                DynamicUrl? teamsChannel = await _dynamicUrlRepository.FirstOrDefaultAsync(q => q.KeyName == keyName);
                if (teamsChannel?.Url == null)
                {
                    _logger.LogWarning("Teams channel not found for key {KeyName}", keyName);
                    return string.Empty;
                }

                _logger.LogDebug("Resolved Teams channel for key {KeyName}: {Url}", keyName, teamsChannel.Url);
                return teamsChannel.Url;
        }

        [RemoteService(false)]
        public async Task NotifyChefsEventToTeamsAsync(string factName, string factValue, bool alert = false)
        {
            string teamsChannel = await InitializeTeamsChannelAsync(alert ? TeamsNotificationService.TEAMS_ALERT : TeamsNotificationService.TEAMS_NOTIFICATION);
            if (teamsChannel.IsNullOrEmpty())
            {
                return;
            }

            string? envInfo = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string activityTitle = "Chefs Submission Event Validation Error";
            string activitySubtitle = "Environment: " + envInfo;
            _teamsNotificationService.AddFact(factName, factValue);
            await _teamsNotificationService.PostFactsToTeamsAsync(teamsChannel, activityTitle, activitySubtitle);
        }

        public async Task PostToTeamsAsync(string activityTitle, string activitySubtitle, List<Fact> facts)
        {
            string teamsChannel = await InitializeTeamsChannelAsync(TeamsNotificationService.TEAMS_NOTIFICATION);
            if (teamsChannel.IsNullOrEmpty())
            {
                _logger.LogWarning("PostToTeamsAsync: no Teams channel configured, skipping notification {Title}", activityTitle);
                return;
            }

            string messageCard = TeamsNotificationService.InitializeMessageCard(activityTitle, activitySubtitle, facts);
            try
            {
                _logger.LogDebug("Posting Teams notification to {Channel} with title {Title} and {FactCount} facts", teamsChannel, activityTitle, facts?.Count ?? 0);
                await TeamsNotificationService.PostToTeamsChannelAsync(teamsChannel, messageCard);
                _logger.LogInformation("Posted Teams notification '{Title}' to channel {Channel}", activityTitle, teamsChannel);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to post Teams notification '{Title}' to channel {Channel}", activityTitle, teamsChannel);
            }
        }

        public async Task PostToTeamsAsync(string activityTitle, string activitySubtitle)
        {
            string teamsChannel = await InitializeTeamsChannelAsync(TeamsNotificationService.TEAMS_NOTIFICATION);
            if (teamsChannel.IsNullOrEmpty())
            {
                _logger.LogWarning("PostToTeamsAsync (no-facts): no Teams channel configured, skipping notification {Title}", activityTitle);
                return;
            }

            List<Fact> facts = new() { };
            string messageCard = TeamsNotificationService.InitializeMessageCard(activityTitle, activitySubtitle, facts);
            try
            {
                _logger.LogDebug("Posting Teams notification (no-facts) to {Channel} with title {Title}", teamsChannel, activityTitle);
                await TeamsNotificationService.PostToTeamsChannelAsync(teamsChannel, messageCard);
                _logger.LogInformation("Posted Teams notification '{Title}' to channel {Channel}", activityTitle, teamsChannel);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to post Teams notification '{Title}' to channel {Channel}", activityTitle, teamsChannel);
            }
        }

        public async Task PostChefsEventToTeamsAsync(string subscriptionEvent, dynamic form, dynamic chefsFormVersion)
        {
            string teamsChannel = await InitializeTeamsChannelAsync(TeamsNotificationService.TEAMS_NOTIFICATION);
            if (teamsChannel.IsNullOrEmpty())
            {
                return;
            }
            await TeamsNotificationService.PostChefsEventToTeamsAsync(teamsChannel, subscriptionEvent, form, chefsFormVersion);
        }
    }
}