using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Identity;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Integration;
using Volo.Abp.Users;

namespace Unity.Payments.PaymentRequests.Notifications
{
    /// <summary>
    /// Email recipient strategy that collects emails from users with the Financial Analyst role.
    /// Automatically discovered via reflection and registered in PaymentsApplicationModule.
    /// </summary>
    [ExposeServices(typeof(IEmailRecipientStrategy))]
    public class FinancialAnalystEmailStrategy : IEmailRecipientStrategy, ITransientDependency
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
                var usersResult = await _identityUserLookupAppService.SearchAsync(new UserLookupSearchInputDto());
                var users = usersResult.Items?.Cast<IUserData>() ?? [];
                
                foreach (var user in users)
                {
                    try
                    {
                        var roles = await _identityUserLookupAppService.GetRoleNamesAsync(user.Id);
                        if (roles != null && roles.Contains(UnityRoles.FinancialAnalyst) && !string.IsNullOrWhiteSpace(user.Email))
                        {
                            financialAnalystEmails.Add(user.Email);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogWarning(ex, "FinancialAnalystEmailStrategy: Failed to get roles for a user.");
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