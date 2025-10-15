using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Services;

namespace Unity.Payments.PaymentRequests.Notifications
{
    /// <summary>
    /// Factory for discovering and providing email recipient strategies.
    /// Strategies are automatically discovered via reflection and registered in PaymentsApplicationModule.
    /// </summary>
    public class EmailRecipientStrategyFactory : ApplicationService
    {
        private readonly IServiceProvider _serviceProvider;

        public EmailRecipientStrategyFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets all registered email recipient strategies.
        /// Strategies are automatically discovered via reflection and registered in PaymentsApplicationModule.
        /// </summary>
        /// <returns>List of all available email recipient strategies</returns>
        public List<IEmailRecipientStrategy> GetAllStrategies()
        {
            try
            {
                var strategies = _serviceProvider.GetServices<IEmailRecipientStrategy>().ToList();
                
                Logger.LogInformation("EmailRecipientStrategyFactory: Discovered {Count} email recipient strategies: {StrategyNames}", 
                    strategies.Count, 
                    string.Join(", ", strategies.Select(s => s.StrategyName)));
                
                return strategies;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "EmailRecipientStrategyFactory: Error discovering email recipient strategies");
                return [];
            }
        }

        /// <summary>
        /// Gets a specific strategy by name.
        /// </summary>
        /// <param name="strategyName">Name of the strategy to retrieve</param>
        /// <returns>The strategy if found, null otherwise</returns>
        public IEmailRecipientStrategy? GetStrategy(string strategyName)
        {
            return GetAllStrategies().Find(s =>
                string.Equals(s.StrategyName, strategyName, StringComparison.OrdinalIgnoreCase));
        }
    }
}