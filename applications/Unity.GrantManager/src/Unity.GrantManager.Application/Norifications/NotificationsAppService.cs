using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public NotificationsAppService(IDynamicUrlRepository dynamicUrlRepository)
        {
            _dynamicUrlRepository = dynamicUrlRepository;
            _teamsNotificationService = new TeamsNotificationService();
        }

        public async Task<string> InitializeTeamsChannelAsync(string keyName)
        {
            DynamicUrl? teamsChannel = await _dynamicUrlRepository.FirstOrDefaultAsync(q => q.KeyName == keyName);
            if (teamsChannel?.Url == null)
            {
                return "";
            }
            return teamsChannel.Url;
        }

        [RemoteService(false)]
        public async Task NotifyChefsEventToTeamsAsync(string factName, string factValue)
        {
            string teamsChannel = await InitializeTeamsChannelAsync(TeamsNotificationService.TEAMS_NOTIFICATION);
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
                return;
            }

            string messageCard = TeamsNotificationService.InitializeMessageCard(activityTitle, activitySubtitle, facts);
            await TeamsNotificationService.PostToTeamsChannelAsync(teamsChannel, messageCard);
        }

        public async Task PostToTeamsAsync(string activityTitle, string activitySubtitle)
        {
            string teamsChannel = await InitializeTeamsChannelAsync(TeamsNotificationService.TEAMS_NOTIFICATION);
            if (teamsChannel.IsNullOrEmpty())
            {
                return;
            }
            List<Fact> facts = new() { };
            string messageCard = TeamsNotificationService.InitializeMessageCard(activityTitle, activitySubtitle, facts);
            await TeamsNotificationService.PostToTeamsChannelAsync(teamsChannel, messageCard);
        }

        public async Task PostChefsEventToTeamsAsync(string subscriptionEvent, dynamic form, dynamic chefsFormVersion)
        {
            string teamsChannel = await InitializeTeamsChannelAsync(TeamsNotificationService.TEAMS_NOTIFICATION_1);
            if (teamsChannel.IsNullOrEmpty())
            {
                return;
            }
            await TeamsNotificationService.PostChefsEventToTeamsAsync(teamsChannel, subscriptionEvent, form, chefsFormVersion);
        }
    }
}