using Microsoft.EntityFrameworkCore;
using System.Linq;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.EntityFrameworkCore
{
    public static class WorksheetQueryableExtensions
    {
        public static IQueryable<Worksheet> IncludeDetails(this IQueryable<Worksheet> queryable, bool include = true)
        {
            return !include ? queryable : queryable
                .Include(s => s.Sections.OrderBy(s => s.Order))
                    .ThenInclude(s => s.Fields.OrderBy(s => s.Order));
        }

        public static IQueryable<WorksheetInstance> IncludeDetails(this IQueryable<WorksheetInstance> queryable, bool include = true)
        {
            return !include ? queryable : queryable
                .Include(s => s.Values);
        }

        public static IQueryable<Scoresheet> IncludeDetails(this IQueryable<Scoresheet> queryable, bool include = true)
        {
            return !include ? queryable : queryable
                .Include(s => s.Sections.OrderBy(s => s.Order))
                    .ThenInclude(s => s.Fields.OrderBy(s => s.Order));
        }

        public static IQueryable<ScoresheetSection> IncludeDetails(this IQueryable<ScoresheetSection> queryable, bool include = true)
        {
            return !include ? queryable : queryable
                .Include(s => s.Fields.OrderBy(s => s.Order));
        }
    }
}
