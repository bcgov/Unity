using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Identity;
using Volo.Abp.Identity;

namespace Unity.Payments.PaymentRequests.Notifications
{
    /// <summary>
    /// Email recipient strategy that collects emails from users with the Financial Analyst role.
    /// Automatically discovered via reflection and registered in PaymentsApplicationModule.
    /// </summary>
    public class FinancialAnalystEmailStrategy : IEmailRecipientStrategy
    {
        private readonly IIdentityUserRepository _identityUserRepository;
        private readonly ILogger<FinancialAnalystEmailStrategy> _logger;

        public string StrategyName => "FinancialAnalyst";

        public FinancialAnalystEmailStrategy(ILogger<FinancialAnalystEmailStrategy> logger, IIdentityUserRepository identityUserRepository)
        {
            _logger = logger;
            _identityUserRepository = identityUserRepository;
        }

        public async Task<List<string>> GetEmailRecipientsAsync()
        {
            List<string> financialAnalystEmails = [];

            try
            {
                // Strategy obtains its own users from the identity repository
                // Filter users by FinancialAnalyst role using the repository's efficient method
                var normalizedRoleName = UnityRoles.FinancialAnalyst.ToUpperInvariant();
                var users = await _identityUserRepository.GetListByNormalizedRoleNameAsync(normalizedRoleName);

                financialAnalystEmails = [.. users
                    .Where(user => !string.IsNullOrWhiteSpace(user.Email))
                    .Select(user => user.Email)];
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "FinancialAnalystEmailStrategy: Failed to search users");
            }

            _logger.LogInformation("FinancialAnalystEmailStrategy: Collected financial analyst emails.");
            return financialAnalystEmails;
        }
    }
}