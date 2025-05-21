using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.TeamsNotifications
{
    public interface ITeamsNotificationService : IApplicationService
    {
        Task PostFactsToTeamsAsync(string activityTitle, string activitySubtitle);
        Task NotifyChefsEventToTeamsAsync(string subscriptionEvent, dynamic form, dynamic chefsFormVersion);
        Task PostToTeamsAsync(string activityTitle, string activitySubtitle, List<Fact> facts);
    }
}