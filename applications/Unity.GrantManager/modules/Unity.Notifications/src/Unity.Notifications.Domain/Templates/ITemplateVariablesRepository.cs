using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Notifications.Templates
{
    public interface ITemplateVariablesRepository : IRepository<TemplateVariable, Guid>
    {
     
    }
}
