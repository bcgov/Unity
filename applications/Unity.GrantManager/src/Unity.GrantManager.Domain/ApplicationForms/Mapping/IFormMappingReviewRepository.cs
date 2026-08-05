using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public interface IFormMappingReviewRepository : IRepository<FormMappingReview, Guid>
{
    Task<FormMappingReview?> FindByFormVersionAsync(Guid formVersionId);
}
