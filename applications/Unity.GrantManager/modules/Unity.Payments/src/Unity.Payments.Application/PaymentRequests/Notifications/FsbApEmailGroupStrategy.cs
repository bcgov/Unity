using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Notifications.EmailGroups;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace Unity.Payments.PaymentRequests.Notifications
{
    /// <summary>
    /// Service that collects emails from the "FSB-AP" email group.
    /// Used exclusively for sending notifications when payments reach FSB status.
    /// Does NOT implement IEmailRecipientStrategy to avoid being picked up by other notification services.
    /// </summary>
    public class FsbApEmailGroupStrategy : ISingletonDependency
    {
        private readonly IEmailGroupsRepository _emailGroupsRepository;
        private readonly IEmailGroupUsersRepository _emailGroupUsersRepository;
        private readonly IIdentityUserRepository _identityUserRepository;
        private readonly ILogger<FsbApEmailGroupStrategy> _logger;
        private const string FsbApEmailGroupName = "FSB-AP";

        public FsbApEmailGroupStrategy(
            IEmailGroupsRepository emailGroupsRepository,
            IEmailGroupUsersRepository emailGroupUsersRepository,
            IIdentityUserRepository identityUserRepository,
            ILogger<FsbApEmailGroupStrategy> logger)
        {
            _emailGroupsRepository = emailGroupsRepository;
            _emailGroupUsersRepository = emailGroupUsersRepository;
            _identityUserRepository = identityUserRepository;
            _logger = logger;
        }

        public async Task<List<string>> GetEmailRecipientsAsync()
        {
            List<string> fsbApEmails = [];

            try
            {
                string normalizedFsbApGroupName = FsbApEmailGroupName.ToUpperInvariant();
                var fsbApGroup = (await _emailGroupsRepository.GetListAsync(group =>
                    group.Name != null && group.Name.ToUpper() == normalizedFsbApGroupName)).FirstOrDefault();

                if (fsbApGroup == null)
                {
                    _logger.LogWarning("FsbApEmailGroupStrategy: No FSB-AP email group found.");
                    return fsbApEmails;
                }

                var groupUsers = await _emailGroupUsersRepository.GetListAsync(groupUser => groupUser.GroupId == fsbApGroup.Id);

                if (groupUsers == null || groupUsers.Count == 0)
                {
                    _logger.LogWarning("FsbApEmailGroupStrategy: No users found in FSB-AP email group.");
                    return fsbApEmails;
                }

                // Only fetch users whose IDs are in the FSB-AP group
                var fsbApUserIds = groupUsers.Select(groupUser => groupUser.UserId).Distinct().ToArray();
                var users = await _identityUserRepository.GetListByIdsAsync(fsbApUserIds);

                Dictionary<Guid, string> userEmailLookup = users
                    .Where(user => !string.IsNullOrWhiteSpace(user.Email))
                    .GroupBy(user => user.Id)
                    .Select(group => group.First())
                    .ToDictionary(user => user.Id, user => user.Email!);

                foreach (Guid userId in groupUsers.Select(groupUser => groupUser.UserId).Distinct())
                {
                    if (userEmailLookup.TryGetValue(userId, out string? email) && !string.IsNullOrWhiteSpace(email))
                    {
                        fsbApEmails.Add(email);
                    }
                    else
                    {
                        _logger.LogWarning("FsbApEmailGroupStrategy: No email found for a user in FSB-AP email group.");
                    }
                }

                _logger.LogInformation("FsbApEmailGroupStrategy: Successfully found {Count} emails from FSB-AP email group.", fsbApEmails.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FsbApEmailGroupStrategy: Error retrieving emails from FSB-AP email group.");
            }

            return fsbApEmails;
        }
    }
}
