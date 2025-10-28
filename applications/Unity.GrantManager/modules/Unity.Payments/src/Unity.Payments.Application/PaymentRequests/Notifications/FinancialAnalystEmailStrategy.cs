using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Identity;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Integration;
using Volo.Abp.Users;

namespace Unity.Payments.PaymentRequests.Notifications
{
    /// <summary>
    /// Email recipient strategy that collects emails from users with the Financial Analyst role.
    /// Automatically discovered via reflection and registered in PaymentsApplicationModule.
    /// </summary>
    public class FinancialAnalystEmailStrategy : IEmailRecipientStrategy
    {
        private readonly IIdentityUserIntegrationService _identityUserLookupAppService;
        private readonly ILogger<FinancialAnalystEmailStrategy> _logger;

        public string StrategyName => "FinancialAnalyst";

        public FinancialAnalystEmailStrategy(ILogger<FinancialAnalystEmailStrategy> logger, IIdentityUserIntegrationService identityUserIntegrationService)
        {
            _logger = logger;
            _identityUserLookupAppService = identityUserIntegrationService;
        }

        public async Task<List<string>> GetEmailRecipientsAsync()
        {
            List<string> financialAnalystEmails = [];
            
            try
            {
                // Strategy obtains its own users from the identity service
                // Filter users by FinancialAnalyst role in the initial search to avoid N+1 queries
                var searchInput = new UserLookupSearchInputDto
                {
                    RoleNames = new List<string> { UnityRoles.FinancialAnalyst }
                };
                var usersResult = await _identityUserLookupAppService.SearchAsync(searchInput);
                var users = usersResult.Items?.Cast<IUserData>() ?? [];

                foreach (var user in users)
                {
                    if (!string.IsNullOrWhiteSpace(user.Email))
                    {
                        financialAnalystEmails.Add(user.Email);
                    }
                }
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