using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Notifications.EmailGroups;

public interface IEmailGroupsRepository : IRepository<EmailGroup, Guid>
{
   
}
