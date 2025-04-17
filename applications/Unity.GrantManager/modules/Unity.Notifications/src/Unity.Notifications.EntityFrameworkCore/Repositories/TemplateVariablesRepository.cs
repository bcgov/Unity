using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Notifications.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using System.Collections.Generic;
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
