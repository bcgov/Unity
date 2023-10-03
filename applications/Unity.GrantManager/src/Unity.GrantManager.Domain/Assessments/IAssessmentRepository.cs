using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Assessments
{
    public interface IAssessmentRepository : IRepository<Assessment, Guid>
    {
    }
}
