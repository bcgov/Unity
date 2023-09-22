using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Comments
{
    public interface ICommentsRepository<T> : IRepository<T, Guid> where T : CommentBase
    {
        Task<List<T>> GetListAsync(          
          int skipCount,
          int maxResultCount,
          string sorting,
          string filter);
    }
}
