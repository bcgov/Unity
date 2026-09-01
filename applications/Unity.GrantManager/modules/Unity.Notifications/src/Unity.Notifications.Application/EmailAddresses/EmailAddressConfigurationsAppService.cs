using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Notifications;
using Unity.Notifications.Templates;
using Unity.Notifications.Settings;
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
    IRepository<ScheduledNotification, Guid> scheduledNotificationRepository) : ApplicationService, IEmailAddressConfigurationsAppService
{
    private static readonly string[] AllowedTypes =
    ["Sender", "ReplyTo", "NoReply", "Inbound", "Support", "Other"];

    public async Task<EmailAddressConfigurationDto> CreateAsync(EmailAddressConfigurationDto input)
    {
        Validate(input);
        await EnsureUniqueAsync(input.EmailAddress, input.EmailType, null);

        var entity = await repository.InsertAsync(new EmailAddressConfiguration(
            GuidGenerator.Create(), NormalizeAddress(input.EmailAddress), input.EmailType, input.Description.Trim()));
        entity.IsActive = input.IsActive;
        await repository.UpdateAsync(entity, true);

        return await ToDtoAsync(entity);
    }

    public async Task<EmailAddressConfigurationDto> UpdateAsync(EmailAddressConfigurationDto input)
    {
        Validate(input);
        await EnsureUniqueAsync(input.EmailAddress, input.EmailType, input.Id);

        var entity = await repository.GetAsync(input.Id, true);
        entity.EmailAddress = NormalizeAddress(input.EmailAddress);
        entity.EmailType = input.EmailType;
        entity.Description = input.Description.Trim();
        entity.IsActive = input.IsActive;
        await repository.UpdateAsync(entity, true);

        return await ToDtoAsync(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        if (await IsInUseAsync(entity))
        {
            throw new BusinessException(
                "Unity.Notifications:EmailAddressInUse",
                "This email address must first be removed from the associated configuration before it can be deleted.");
        }

        await repository.DeleteAsync(id, true);
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
            throw new BusinessException("Unity.Notifications:InvalidEmailAddress", "The email address is not valid.");
        }

        if (!AllowedTypes.Contains(input.EmailType, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessException("Unity.Notifications:InvalidEmailType", "The email type is not valid.");
        }

        if (string.Equals(input.EmailType, "Other", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(input.Description))
        {
            throw new BusinessException("Unity.Notifications:EmailDescriptionRequired", "A description is required for Other email addresses.");
        }
    }

    private async Task EnsureUniqueAsync(string address, string emailType, Guid? excludedId)
    {
        var normalizedAddress = NormalizeAddress(address);
        var entities = await repository.GetListAsync(configuration =>
            configuration.EmailAddress == normalizedAddress &&
            configuration.EmailType.ToUpper() == emailType.ToUpper() &&
            (!excludedId.HasValue || configuration.Id != excludedId.Value));

        if (entities.Count > 0)
        {
            throw new BusinessException("Unity.Notifications:DuplicateEmailAddressConfiguration", "This email address and type already exists.");
        }
    }

    private async Task<bool> IsInUseAsync(EmailAddressConfiguration entity)
    {
        var address = NormalizeAddress(entity.EmailAddress);
        var templates = await templatesRepository.GetListAsync();
        if (templates.Any(template => template.SendFrom != null &&
            string.Equals(NormalizeAddress(template.SendFrom), address, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var scheduledNotifications = await scheduledNotificationRepository.GetListAsync();
        if (scheduledNotifications.Any(notification =>
            string.Equals(NormalizeAddress(notification.RecipientIdentifier ?? string.Empty), address, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return string.Equals(entity.EmailType, "Sender", StringComparison.OrdinalIgnoreCase) && entity.IsActive;
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
            IsInUse = await IsInUseAsync(entity)
        };
    }

    private static string NormalizeAddress(string address) => address.Trim().ToLowerInvariant();
}
