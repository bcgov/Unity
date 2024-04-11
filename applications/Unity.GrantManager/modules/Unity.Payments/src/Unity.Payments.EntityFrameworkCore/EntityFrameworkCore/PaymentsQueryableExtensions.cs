using Microsoft.EntityFrameworkCore;
using System.Linq;
using Unity.Payments.BatchPaymentRequests;
using Unity.Payments.Suppliers;

namespace Unity.Payments.EntityFrameworkCore
{
    public static class PaymentsQueryableExtensions
    {
        public static IQueryable<BatchPaymentRequest> IncludeDetails(this IQueryable<BatchPaymentRequest> queryable, bool include = true)
        {
            return !include ? queryable : queryable
                .Include(x => x.PaymentRequests)
                .Include(y => y.ExpenseApprovals);
        }

        public static IQueryable<Supplier> IncludeDetails(this IQueryable<Supplier> queryable, bool include = true)
        {
            return !include ? queryable : queryable
                .Include(x => x.Sites);
        }
    }
}
