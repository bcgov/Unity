using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.Notifications.EmailAddresses;

public interface IEmailAddressConfigurationsAppService
{
    Task<EmailAddressConfigurationDto> CreateAsync(EmailAddressConfigurationDto input);
    Task<EmailAddressConfigurationDto> UpdateAsync(EmailAddressConfigurationDto input);
    Task DeleteAsync(Guid id);
    Task<List<EmailAddressConfigurationDto>> GetListAsync();
}
