using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Assessments;

public interface IAssessmentCommentsRepository : IRepository<AssessmentComment, Guid>
{
    Task<List<AssessmentComment>> GetListAsync(
        int skipCount,
        int maxResultCount,
        string sorting,
        string filter
    );
}