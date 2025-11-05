using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface ISequenceRepository : IRepository
{
    /// <summary>
    /// Gets the next sequential number for a given prefix within the current tenant.
    /// Uses tenant-specific PostgreSQL sequences to ensure uniqueness.
    /// </summary>
    /// <param name="prefix">The prefix for the sequence (e.g., "CGG-")</param>
    /// <returns>The next sequential number for this tenant+prefix combination</returns>
    Task<long> GetNextSequenceNumberAsync(string prefix);
}