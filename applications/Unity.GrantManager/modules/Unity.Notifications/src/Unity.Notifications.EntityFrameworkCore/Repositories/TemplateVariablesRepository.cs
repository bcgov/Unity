using System;
using Unity.Notifications.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Unity.Notifications.Templates;




namespace Unity.Notifications.Repositories
{
    public class TemplateVariablesRepository : EfCoreRepository<NotificationsDbContext, TemplateVariable, Guid>, ITemplateVariablesRepository
    {
        public TemplateVariablesRepository(IDbContextProvider<NotificationsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }


    }
}
