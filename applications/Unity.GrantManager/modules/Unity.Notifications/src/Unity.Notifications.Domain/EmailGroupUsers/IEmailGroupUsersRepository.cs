using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Notifications.EmailGroups;

public interface IEmailGroupUsersRepository : IRepository<EmailGroupUser, Guid>
{
   
}
