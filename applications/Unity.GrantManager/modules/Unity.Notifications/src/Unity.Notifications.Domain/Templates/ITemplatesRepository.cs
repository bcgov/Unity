using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Notifications.Templates
{
    public interface ITemplatesRepository : IBasicRepository<EmailTemplate, Guid>
    {
        Task<EmailTemplate?> GetByIdAsync(Guid id, bool includeDetails = false);
        Task<List<EmailTemplate>> GetByTenentIdAsync(Guid? tenentId);
        Task<EmailTemplate?> GetByNameAsync(string name);
    }
}
