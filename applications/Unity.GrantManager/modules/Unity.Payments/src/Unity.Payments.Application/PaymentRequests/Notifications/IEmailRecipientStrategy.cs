using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.Payments.PaymentRequests.Notifications
{
    /// <summary>
    /// Strategy interface for collecting email recipients for payment notifications.
    /// Implementations are automatically discovered via reflection and registered in PaymentsApplicationModule.
    /// Each strategy is responsible for obtaining emails from its own data source.
    /// </summary>
    public interface IEmailRecipientStrategy
    {
        /// <summary>
        /// Unique name identifier for this strategy
        /// </summary>
        string StrategyName { get; }

        /// <summary>
        /// Collects email addresses from this strategy's specific recipient source.
        /// Strategies can obtain emails from any source: users, external APIs, databases, etc.
        /// </summary>
        /// <returns>List of email addresses from this strategy</returns>
        Task<List<string>> GetEmailRecipientsAsync();
    }
}