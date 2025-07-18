using System;
using Unity.Notifications.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Unity.Notifications.EmailGroups;

namespace Unity.Notifications.Repositories
{
    public class EmailGroupUsersRepository : EfCoreRepository<NotificationsDbContext,EmailGroupUser, Guid>, IEmailGroupUsersRepository
    {
        public EmailGroupUsersRepository(IDbContextProvider<NotificationsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

    }
}
