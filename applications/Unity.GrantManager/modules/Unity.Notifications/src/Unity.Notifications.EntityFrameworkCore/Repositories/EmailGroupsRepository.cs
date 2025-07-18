using System;
using Unity.Notifications.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Unity.Notifications.EmailGroups;


namespace Unity.Notifications.Repositories
{
    public class EmailGroupsRepository : EfCoreRepository<NotificationsDbContext, EmailGroup, Guid>, IEmailGroupsRepository
    {
        public EmailGroupsRepository(IDbContextProvider<NotificationsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
