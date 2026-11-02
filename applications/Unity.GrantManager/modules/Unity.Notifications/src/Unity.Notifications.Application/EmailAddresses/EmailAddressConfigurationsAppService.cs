using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Notifications;
using Unity.Notifications.Emails;
using Unity.Notifications.Templates;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.DependencyInjection;

namespace Unity.Notifications.EmailAddresses;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(EmailAddressConfigurationsAppService), typeof(IEmailAddressConfigurationsAppService))]
public class EmailAddressConfigurationsAppService(
    IEmailAddressConfigurationsRepository repository,
    ITemplatesRepository templatesRepository,
    IEmailLogsRepository emailLogsRepository) : ApplicationService, IEmailAddressConfigurationsAppService
{
    private static readonly string[] AllowedTypes =
    ["Sender", "ReplyTo", "NoReply", "Inbound", "Support", "Other"];

    public async Task<EmailAddressConfigurationDto> CreateAsync(EmailAddressConfigurationDto input)
    {
        Validate(input);
        var emailType = NormalizeEmailType(input.EmailType);
        await EnsureUniqueAsync(input.EmailAddress, emailType, null);
        var existing = await repository.GetListAsync();
        var isDefault = input.IsDefault || existing.All(configuration => !configuration.IsDefault);
        EnsureDefaultIsActive(isDefault, input.IsActive);
        if (isDefault)
        {
            await ClearDefaultsAsync(existing);
        }

        var entity = await repository.InsertAsync(new EmailAddressConfiguration(
            GuidGenerator.Create(), NormalizeAddress(input.EmailAddress), emailType, input.Description.Trim(), isDefault));
        entity.IsActive = input.IsActive;
        await repository.UpdateAsync(entity, true);

        return await ToDtoAsync(entity);
    }

    public async Task<EmailAddressConfigurationDto> UpdateAsync(EmailAddressConfigurationDto input)
    {
        Validate(input);
        var emailType = NormalizeEmailType(input.EmailType);
        var entity = await repository.GetAsync(input.Id, true);

        // Only check uniqueness when the address or type actually changes, so rows that are already
        // duplicated (legacy data) can still be edited and cleaned up.
        var identityChanged =
            NormalizeAddress(entity.EmailAddress) != NormalizeAddress(input.EmailAddress) ||
            !string.Equals(entity.EmailType, emailType, StringComparison.OrdinalIgnoreCase);
        if (identityChanged)
        {
            await EnsureUniqueAsync(input.EmailAddress, emailType, input.Id);
        }

        EnsureDefaultIsActive(input.IsDefault, input.IsActive);
        if (entity.IsDefault && !input.IsDefault)
        {
            throw new BusinessException(NotificationsErrorCodes.DefaultEmailAddressRequired);
        }
        if (entity.IsDefault && !input.IsActive)
        {
            throw new BusinessException(NotificationsErrorCodes.DefaultEmailAddressRequired);
        }

        var existing = await repository.GetListAsync();
        if (!input.IsDefault && existing.Where(configuration => configuration.Id != entity.Id).All(configuration => !configuration.IsDefault))
        {
            throw new BusinessException(NotificationsErrorCodes.DefaultEmailAddressRequired);
        }
        if (input.IsDefault)
        {
            await ClearDefaultsAsync(existing.Where(configuration => configuration.Id != entity.Id));
        }

        if (identityChanged)
        {
            // Keep the stored casing otherwise; lowercasing a legacy duplicate would collide on the unique index
            entity.EmailAddress = NormalizeAddress(input.EmailAddress);
        }
        entity.EmailType = emailType;
        entity.Description = input.Description.Trim();
        entity.IsActive = input.IsActive;
        entity.IsDefault = input.IsDefault;
        await repository.UpdateAsync(entity, true);

        return await ToDtoAsync(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);

        // A default row that is a duplicate of another active row (legacy data) can be removed by
        // handing the default over to that duplicate, since the effective sender address is unchanged.
        EmailAddressConfiguration? defaultSuccessor = null;
        if (entity.IsDefault)
        {
            defaultSuccessor = (await GetDuplicatesAsync(entity)).FirstOrDefault(configuration => configuration.IsActive)
                ?? throw new BusinessException(NotificationsErrorCodes.DefaultEmailAddressRequired);
        }
        if (await IsInUseAsync(entity))
        {
            throw new BusinessException(NotificationsErrorCodes.EmailAddressInUse);
        }

        await repository.DeleteAsync(id, true);

        if (defaultSuccessor != null)
        {
            defaultSuccessor.IsDefault = true;
            await repository.UpdateAsync(defaultSuccessor, true);
        }
    }

    public async Task<List<EmailAddressConfigurationDto>> GetListAsync()
    {
        var entities = await repository.GetListAsync();
        var result = new List<EmailAddressConfigurationDto>(entities.Count);
        foreach (var entity in entities)
        {
            result.Add(await ToDtoAsync(entity));
        }

        return result;
    }

    private static void Validate(EmailAddressConfigurationDto input)
    {
        if (!new EmailAddressAttribute().IsValid(input.EmailAddress))
        {
            throw new BusinessException(NotificationsErrorCodes.InvalidEmailAddress);
        }

        if (!AllowedTypes.Contains(input.EmailType, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessException(NotificationsErrorCodes.InvalidEmailType);
        }

        if (string.Equals(input.EmailType, "Other", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(input.Description))
        {
            throw new BusinessException(NotificationsErrorCodes.EmailDescriptionRequired);
        }
    }

    private static void EnsureDefaultIsActive(bool isDefault, bool isActive)
    {
        if (isDefault && !isActive)
        {
            throw new BusinessException(NotificationsErrorCodes.DefaultEmailAddressRequired);
        }
    }

    private async Task ClearDefaultsAsync(IEnumerable<EmailAddressConfiguration> configurations)
    {
        foreach (var configuration in configurations.Where(configuration => configuration.IsDefault))
        {
            configuration.IsDefault = false;
            await repository.UpdateAsync(configuration, true);
        }
    }

    private async Task EnsureUniqueAsync(string address, string emailType, Guid? excludedId)
    {
        // Stored addresses are not guaranteed to be lowercase (e.g. the seeded default sender keeps its original casing)
        var normalizedAddress = NormalizeAddress(address);
        var entities = await repository.GetListAsync(configuration =>
            configuration.EmailAddress.ToLower() == normalizedAddress &&
            configuration.EmailType.ToUpper() == emailType.ToUpper() &&
            (!excludedId.HasValue || configuration.Id != excludedId.Value));

        if (entities.Count > 0)
        {
            throw new BusinessException(NotificationsErrorCodes.DuplicateEmailAddressConfiguration);
        }
    }

    private async Task<bool> IsInUseAsync(EmailAddressConfiguration entity)
    {
        // Usage is tracked by address, not by configuration. If another configuration with the same
        // address and type exists, removing this one leaves nothing that relied on it unconfigured.
        if ((await GetDuplicatesAsync(entity)).Count > 0)
        {
            return false;
        }

        var address = NormalizeAddress(entity.EmailAddress);
        var templates = await templatesRepository.GetListAsync();
        if (templates.Any(template => template.SendFrom != null &&
            string.Equals(NormalizeAddress(template.SendFrom), address, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var emailLogs = await emailLogsRepository.GetListAsync(emailLog =>
            emailLog.FromAddress.ToLower() == address);
        if (emailLogs.Count > 0)
        {
            return true;
        }

        return false;
    }

    private async Task<List<EmailAddressConfiguration>> GetDuplicatesAsync(EmailAddressConfiguration entity)
    {
        var address = NormalizeAddress(entity.EmailAddress);
        return await repository.GetListAsync(configuration =>
            configuration.Id != entity.Id &&
            configuration.EmailAddress.ToLower() == address &&
            configuration.EmailType.ToUpper() == entity.EmailType.ToUpper());
    }

    private async Task<EmailAddressConfigurationDto> ToDtoAsync(EmailAddressConfiguration entity)
    {
        return new EmailAddressConfigurationDto
        {
            Id = entity.Id,
            EmailAddress = entity.EmailAddress,
            EmailType = entity.EmailType,
            Description = entity.Description,
            IsActive = entity.IsActive,
            IsDefault = entity.IsDefault,
            IsInUse = await IsInUseAsync(entity)
        };
    }

    private static string NormalizeAddress(string address) => address.Trim().ToLowerInvariant();

    private static string NormalizeEmailType(string emailType) =>
        AllowedTypes.First(type => string.Equals(type, emailType, StringComparison.OrdinalIgnoreCase));
}
