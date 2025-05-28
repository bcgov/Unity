using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Notifications.TeamsNotifications;

namespace Unity.GrantManager.Notifications
{
    public interface INotificationsAppService
    {
        Task NotifyChefsEventToTeamsAsync(string factName, string factValue);
        Task PostChefsEventToTeamsAsync(string subscriptionEvent, dynamic form, dynamic chefsFormVersion);
        Task PostToTeamsAsync(string activityTitle, string activitySubtitle);
        Task PostToTeamsAsync(string activityTitle, string activitySubtitle, List<Fact> facts);
    }
}
