using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.Notifications.EmailGroups;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using Volo.Abp.Identity.Integration;
using System;
using Volo.Abp.Identity;

namespace Unity.Payments.PaymentRequests.Notifications
{
    /// <summary>
    /// Email recipient strategy that collects emails from the "Payments" email group.
    /// Automatically discovered via reflection and registered in PaymentsApplicationModule.
    /// </summary>
    public class PaymentsEmailGroupStrategy : ApplicationService, IEmailRecipientStrategy
    {
        private readonly IEmailGroupsRepository _emailGroupsRepository;
        private readonly IEmailGroupUsersRepository _emailGroupUsersRepository;
        private readonly IIdentityUserIntegrationService _identityUserLookupAppService;
        private const string PaymentsEmailGroupName = "Payments";

        public string StrategyName => "PaymentsEmailGroup";

        public PaymentsEmailGroupStrategy(
            IEmailGroupsRepository emailGroupsRepository,
            IEmailGroupUsersRepository emailGroupUsersRepository,
            IIdentityUserIntegrationService identityUserIntegrationService)
        {
            _emailGroupsRepository = emailGroupsRepository;
            _emailGroupUsersRepository = emailGroupUsersRepository;
            _identityUserLookupAppService = identityUserIntegrationService;
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
                    Logger.LogWarning("PaymentsEmailGroupStrategy: No '{GroupName}' email group found", PaymentsEmailGroupName);
                    return paymentsEmails;
                }

                var groupUsers = await _emailGroupUsersRepository.GetListAsync(groupUser => groupUser.GroupId == paymentsGroup.Id);
                if (groupUsers == null || groupUsers.Count == 0)
                {
                    Logger.LogWarning("PaymentsEmailGroupStrategy: No users found in '{GroupName}' email group", PaymentsEmailGroupName);
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
                        Logger.LogWarning("PaymentsEmailGroupStrategy: No email found for a user in '{GroupName}' email group.", PaymentsEmailGroupName);
                    }
                }

                Logger.LogInformation("PaymentsEmailGroupStrategy: Found {Count} emails from '{GroupName}' group", paymentsEmails.Count, PaymentsEmailGroupName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "PaymentsEmailGroupStrategy: Error retrieving emails from '{GroupName}' group", PaymentsEmailGroupName);
            }

            return paymentsEmails;
        }
    }
}