using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Notifications.EmailAddresses;

public interface IEmailAddressConfigurationsRepository : IRepository<EmailAddressConfiguration, Guid>
{
}
