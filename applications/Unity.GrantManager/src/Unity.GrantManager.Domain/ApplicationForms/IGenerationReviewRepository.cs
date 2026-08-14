using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.ApplicationForms;

public interface IGenerationReviewRepository : IRepository<GenerationReview, Guid>
{
    Task<GenerationReview?> FindLatestByOperationAndFormVersionAsync(
        string operation,
        Guid formVersionId);

    Task<List<GenerationReview>> GetListByOperationAndFormVersionAsync(
        string operation,
        Guid formVersionId);
}
