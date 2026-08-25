using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Services;

namespace Unity.Notifications.ReadStates;

public class NotificationReadStateManager(
    INotificationReadStateRepository notificationReadStateRepository) : DomainService
{
    public async Task<DateTime> GetLastReadAtAsync(Guid userId, Guid? tenantId)
    {
        var state = await FindAsync(userId, tenantId);

        return state?.LastReadAt ?? DateTime.MinValue;
    }

    public async Task MarkReadAsync(Guid userId, Guid? tenantId)
    {
        var state = await FindAsync(userId, tenantId);

        if (state == null)
        {
            await notificationReadStateRepository.InsertAsync(new NotificationReadState
            {
                TenantId = tenantId,
                UserId = userId,
                LastReadAt = Clock.Now
            }, autoSave: true);
        }
        else
        {
            state.LastReadAt = Clock.Now;
            await notificationReadStateRepository.UpdateAsync(state, autoSave: true);
        }
    }

    private async Task<NotificationReadState?> FindAsync(Guid userId, Guid? tenantId)
    {
        var query = await notificationReadStateRepository.GetQueryableAsync();

        return await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(x => x.UserId == userId && x.TenantId == tenantId));
    }
}
