using Microsoft.EntityFrameworkCore;
using System.Linq;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.Suppliers;

namespace Unity.Payments.EntityFrameworkCore
{
    public static class PaymentsQueryableExtensions
    {
        public static IQueryable<PaymentRequest> IncludeDetails(this IQueryable<PaymentRequest> queryable, bool include = true)
        {
            return !include ? queryable : queryable
                .Include(s => s.Site)
                .Include(y => y.ExpenseApprovals);                
        }

        public static IQueryable<Supplier> IncludeDetails(this IQueryable<Supplier> queryable, bool include = true)
        {
            return !include ? queryable : queryable
                .Include(x => x.Sites);
        }
    }
}
