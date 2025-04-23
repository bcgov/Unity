using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Notifications.Templates
{
    public interface ITemplateVariablesRepository : IRepository<TemplateVariable, Guid>
    {
     
    }
}
