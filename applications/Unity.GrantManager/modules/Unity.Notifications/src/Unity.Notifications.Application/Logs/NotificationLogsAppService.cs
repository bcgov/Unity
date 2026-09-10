using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Uow;

namespace Unity.Notifications.Logs;

[RemoteService(false)]
public class NotificationLogsAppService(
    INotificationLogsRepository notificationLogsRepository,
    ILocalEventBus localEventBus,
    IUnitOfWorkManager unitOfWorkManager)
    : NotificationsAppService, INotificationLogsAppService
{
    public async Task<Guid> CreateAsync(CreateNotificationLogDto input)
    {
        using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);

        var entity = new NotificationLog
        {
            TenantId = input.TenantId,
            UserId = input.UserId,
            SenderUserId = input.SenderUserId,
            SenderDisplayName = input.SenderDisplayName,
            NotificationType = input.NotificationType,
            Channel = input.Channel,
            Severity = input.Severity,
            Title = input.Title,
            Message = input.Message,
            Source = input.Source,
            SourceReference = input.SourceReference,
            PayloadJson = input.PayloadJson,
            CorrelationId = input.CorrelationId,
            IsDeliveredRealtime = input.IsDeliveredRealtime,
            DeliveryTarget = input.DeliveryTarget,
            ExceptionType = input.ExceptionType,
            ExceptionMessage = input.ExceptionMessage,
            StackExcerpt = input.StackExcerpt,
            CommitSha = input.CommitSha,
            Environment = input.Environment
        };

        entity = await notificationLogsRepository.InsertAsync(entity, autoSave: true);

        await localEventBus.PublishAsync(new NotificationLogCreatedEto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            UserId = entity.UserId,
            NotificationType = entity.NotificationType,
            Channel = entity.Channel,
            Severity = entity.Severity,
            Title = entity.Title,
            Message = entity.Message,
            Source = entity.Source,
            CorrelationId = entity.CorrelationId,
            CreationTime = entity.CreationTime
        });

        await uow.CompleteAsync();

        return entity.Id;
    }
}