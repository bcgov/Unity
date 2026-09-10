using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Notifications.Logs;

namespace Unity.GrantManager.Notifications
{
    public interface INotificationsAppService
    {
        Task NotifyChefsEventToTeamsAsync(string factName, string factValue, bool alert = false);
        Task PostChefsEventToTeamsAsync(string subscriptionEvent, dynamic form, dynamic chefsFormVersion);
        Task PostToNotificationsAsync(string activityTitle, string activitySubtitle);
        Task PostToNotificationsAsync(string activityTitle, string activitySubtitle, List<Fact> facts);
    }
}
