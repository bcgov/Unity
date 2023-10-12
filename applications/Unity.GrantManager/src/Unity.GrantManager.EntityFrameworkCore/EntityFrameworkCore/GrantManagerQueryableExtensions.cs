using Microsoft.EntityFrameworkCore;
using System.Linq;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.EntityFrameworkCore;
public static class GrantManagerQueryableExtensions
{
    public static IQueryable<Application> IncludeDetails(this IQueryable<Application> queryable, bool include = true)
    {
        return !include ? queryable : queryable.Include(x => x.ApplicationStatus);
    }
}
