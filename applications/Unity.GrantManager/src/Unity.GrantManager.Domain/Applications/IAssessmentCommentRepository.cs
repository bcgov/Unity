using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IAssessmentCommentRepository : IRepository<AssessmentComment, Guid>
{
    Task<List<AssessmentComment>> GetListAsync(
        int skipCount,
        int maxResultCount,
        string sorting,
        string filter
    );
}