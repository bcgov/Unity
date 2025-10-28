using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Notifications.EmailGroups;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Integration;
using Volo.Abp.Users;

namespace Unity.Payments.PaymentRequests.Notifications
{
    /// <summary>
    /// Email recipient strategy that collects emails from the "Payments" email group.
    /// Automatically discovered via reflection and registered in PaymentsApplicationModule.
    /// </summary>
    public class PaymentsEmailGroupStrategy : IEmailRecipientStrategy
    {
        private readonly IEmailGroupsRepository _emailGroupsRepository;
        private readonly IEmailGroupUsersRepository _emailGroupUsersRepository;
        private readonly IIdentityUserIntegrationService _identityUserLookupAppService;
        private readonly ILogger<PaymentsEmailGroupStrategy> _logger;
        private const string PaymentsEmailGroupName = "Payments";

        public string StrategyName => "PaymentsEmailGroup";

        public PaymentsEmailGroupStrategy(
            IEmailGroupsRepository emailGroupsRepository,
            IEmailGroupUsersRepository emailGroupUsersRepository,
            IIdentityUserIntegrationService identityUserIntegrationService,
            ILogger<PaymentsEmailGroupStrategy> logger )
        {
            _emailGroupsRepository = emailGroupsRepository;
            _emailGroupUsersRepository = emailGroupUsersRepository;
            _identityUserLookupAppService = identityUserIntegrationService;
            _logger = logger;
        }

        public async Task<List<string>> GetEmailRecipientsAsync()
        {
            List<string> paymentsEmails = [];
            
            try
            {
                string normalizedPaymentsGroupName = PaymentsEmailGroupName.ToUpperInvariant();
                var paymentsGroup = (await _emailGroupsRepository.GetListAsync(group => 
                    group.Name != null && group.Name.ToUpper() == normalizedPaymentsGroupName)).FirstOrDefault();
                
                if (paymentsGroup == null)
                {
                    _logger.LogWarning("PaymentsEmailGroupStrategy: No Payments email group found.");
                    return paymentsEmails;
                }

                var groupUsers = await _emailGroupUsersRepository.GetListAsync(groupUser => groupUser.GroupId == paymentsGroup.Id);

                if (groupUsers == null || groupUsers.Count == 0)
                {
                    _logger.LogWarning("PaymentsEmailGroupStrategy: No users found in Payments email group.");
                    return paymentsEmails;
                }

                // Strategy obtains its own users from the identity service
                var usersResult = await _identityUserLookupAppService.SearchAsync(new UserLookupSearchInputDto());
                var users = usersResult.Items?.Cast<IUserData>() ?? [];

                Dictionary<Guid, string> userEmailLookup = users
                    .Where(user => !string.IsNullOrWhiteSpace(user.Email))
                    .GroupBy(user => user.Id)
                    .Select(group => group.First())
                    .ToDictionary(user => user.Id, user => user.Email);

                foreach (Guid userId in groupUsers.Select(groupUser => groupUser.UserId).Distinct())
                {
                    if (userEmailLookup.TryGetValue(userId, out string? email) && !string.IsNullOrWhiteSpace(email))
                    {
                        paymentsEmails.Add(email);
                    }
                    else
                    {
                        _logger.LogWarning("PaymentsEmailGroupStrategy: No email found for a user in Payments email group.");
                    }
                }

                _logger.LogInformation("PaymentsEmailGroupStrategy: Successfully found emails from Payments email group.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentsEmailGroupStrategy: Error retrieving emails from Payments email group.");
            }

            return paymentsEmails;
        }
    }
}