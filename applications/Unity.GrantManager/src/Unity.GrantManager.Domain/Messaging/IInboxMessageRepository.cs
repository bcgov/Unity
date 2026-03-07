using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Messaging;

public interface IInboxMessageRepository : IRepository<InboxMessage, Guid>
{
    Task<InboxMessage?> FindByMessageIdAsync(string messageId);
    Task<List<InboxMessage>> GetPendingAsync(string source, int maxCount = 10);
    Task<int> DeleteProcessedOlderThanAsync(DateTime cutoffDate);
}
