using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Assessments;

public interface IAssessmentRepository : IRepository<Assessment, Guid>
{
    Task<bool> ExistsAsync(Guid applicationId, Guid userId);
    Task<List<AssessmentWithAssessorQueryResultItem>> GetListWithAssessorsAsync(Guid applicationId);
}
