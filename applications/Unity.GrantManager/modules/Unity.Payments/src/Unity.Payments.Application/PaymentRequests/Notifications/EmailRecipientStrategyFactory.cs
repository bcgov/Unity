using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.Application.Services;

namespace Unity.Payments.PaymentRequests.Notifications
{
    /// <summary>
    /// Factory for discovering and providing email recipient strategies.
    /// Strategies are automatically discovered via reflection and registered in PaymentsApplicationModule.
    /// </summary>
    public class EmailRecipientStrategyFactory : ApplicationService
    {
        private readonly IEnumerable<IEmailRecipientStrategy> _emailRecipientStrategies;

        public EmailRecipientStrategyFactory(IEnumerable<IEmailRecipientStrategy> emailRecipientStrategies)
        {
            _emailRecipientStrategies = emailRecipientStrategies;
        }

        /// <summary>
        /// Gets all registered email recipient strategies.
        /// Strategies are automatically discovered via reflection and registered in PaymentsApplicationModule.
        /// </summary>
        /// <returns>List of all available email recipient strategies</returns>
        public IEnumerable<IEmailRecipientStrategy> GetAllStrategies()
        {
            return _emailRecipientStrategies;
        }

        /// <summary>
        /// Gets a specific strategy by name.
        /// </summary>
        /// <param name="strategyName">Name of the strategy to retrieve</param>
        /// <returns>The strategy if found, null otherwise</returns>
        public IEmailRecipientStrategy? GetStrategy(string strategyName)
        {
            return GetAllStrategies().FirstOrDefault(s =>
                string.Equals(s.StrategyName, strategyName, StringComparison.OrdinalIgnoreCase));
        }
    }
}