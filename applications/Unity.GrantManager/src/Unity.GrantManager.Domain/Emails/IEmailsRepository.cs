using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Emails
{
    public interface IEmailsRepository : IRepository<ApplicationEmail> 
    {
        Task<List<ApplicationEmail>> GetListAsync(
          int skipCount,
          int maxResultCount,
          string sorting,
          string filter);
    }
}
