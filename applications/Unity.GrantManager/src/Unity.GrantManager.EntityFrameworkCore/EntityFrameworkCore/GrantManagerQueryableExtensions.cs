using Microsoft.EntityFrameworkCore;
using System.Linq;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Locality;

namespace Unity.GrantManager.EntityFrameworkCore;
public static class GrantManagerQueryableExtensions
{
    public static IQueryable<Application> IncludeDetails(this IQueryable<Application> queryable, bool include = true)
    {
        return !include ? queryable : queryable
            .Include(x => x.ApplicationStatus)
            .Include(x => x.Owner)
            .Include(x => x.ApplicationAssignments)
            .Include(x => x.Applicant);
    }

    public static IQueryable<Sector> IncludeDetails(this IQueryable<Sector> queryable, bool include = true)
    {
        if (!include)
        {
            return queryable;
        }

        return queryable
            .Include(x => x.SubSectors);
    }
}
