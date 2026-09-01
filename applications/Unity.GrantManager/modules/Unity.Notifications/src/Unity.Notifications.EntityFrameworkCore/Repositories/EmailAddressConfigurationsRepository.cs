using System;
using Unity.Notifications.EmailAddresses;
using Unity.Notifications.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Notifications.Repositories;

public class EmailAddressConfigurationsRepository(
    IDbContextProvider<NotificationsDbContext> dbContextProvider)
    : EfCoreRepository<NotificationsDbContext, EmailAddressConfiguration, Guid>(dbContextProvider),
        IEmailAddressConfigurationsRepository
{
}
