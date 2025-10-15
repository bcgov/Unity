using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Identity;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Integration;
using Volo.Abp.Users;

namespace Unity.Payments.PaymentRequests.Notifications
{
    /// <summary>
    /// Email recipient strategy that collects emails from users with the Financial Analyst role.
    /// Automatically discovered via reflection and registered in PaymentsApplicationModule.
    /// </summary>
    public class FinancialAnalystEmailStrategy : ApplicationService, IEmailRecipientStrategy
    {
        private readonly IIdentityUserIntegrationService _identityUserLookupAppService;

        public string StrategyName => "FinancialAnalyst";

        public FinancialAnalystEmailStrategy(IIdentityUserIntegrationService identityUserIntegrationService)
        {
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
                        Logger.LogWarning(ex, "FinancialAnalystEmailStrategy: Failed to get roles for a user.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "FinancialAnalystEmailStrategy: Failed to search users");
            }

            Logger.LogInformation("FinancialAnalystEmailStrategy: Found {Count} financial analyst emails", financialAnalystEmails.Count);
            return financialAnalystEmails;
        }
    }
}