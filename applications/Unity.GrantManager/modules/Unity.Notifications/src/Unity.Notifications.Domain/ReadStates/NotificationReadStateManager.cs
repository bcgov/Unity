using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Data;
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
            try
            {
                await notificationReadStateRepository.UpdateAsync(state, autoSave: true);
            }
            catch (AbpDbConcurrencyException)
            {
                // Another page already advanced this user's read marker.
            }
        }
    }

    private async Task<NotificationReadState?> FindAsync(Guid userId, Guid? tenantId)
    {
        var query = await notificationReadStateRepository.GetQueryableAsync();

        return await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(x => x.UserId == userId && x.TenantId == tenantId));
    }
}
