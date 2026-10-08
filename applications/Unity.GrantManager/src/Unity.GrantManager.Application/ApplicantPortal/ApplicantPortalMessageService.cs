using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using Unity.GrantManager.Settings;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;
using Volo.Abp.Validation;

namespace Unity.GrantManager.ApplicantPortal;

// Internal service also used by the API-key-authorized applicant profile query.
// Administrative authorization belongs to ApplicantPortalMessageAppService.
public class ApplicantPortalMessageService(
    ICurrentTenant currentTenant,
    ISettingProvider settingProvider,
    ISettingManager settingManager) : ITransientDependency
{
    public async Task<MultipleIdentitiesMessageDto> GetAsync()
    {
        _ = currentTenant.GetId();
        var setting = await settingProvider.GetOrNullAsync(SettingsConstants.ApplicantPortal.UseDefaultMultipleIdentitiesMessage);
        var useDefault = !bool.TryParse(setting, out var configuredDefault) || configuredDefault;
        var customHtml = await GetCustomMessageAsync();

        // Missing or invalid legacy data must not result in an empty warning.
        return CreateConfiguration(useDefault || customHtml is null, customHtml);
    }

    public async Task<MultipleIdentitiesMessageDto> UpdateAsync(UpdateMultipleIdentitiesMessageDto input)
    {
        _ = currentTenant.GetId();
        ArgumentNullException.ThrowIfNull(input);
        var customHtml = await GetCustomMessageAsync();
        var submittedHtml = input.UseDefaultMessage ? input.CustomMessageHtml : input.MessageHtml ?? string.Empty;
        if (submittedHtml is not null)
        {
            ValidateLength(submittedHtml);
            var html = MultipleIdentitiesMessageContent.Sanitize(submittedHtml);
            ValidateLength(html);
            if (!MultipleIdentitiesMessageContent.HasText(html))
            {
                throw ValidationError("Message is required.");
            }

            customHtml = html;
            await settingManager.SetForCurrentTenantAsync(SettingsConstants.ApplicantPortal.MultipleIdentitiesMessageHtml, customHtml);
        }

        // Default mode changes which template is effective without deleting the
        // custom template. Both writes participate in the application's unit of work.
        await settingManager.SetForCurrentTenantAsync(SettingsConstants.ApplicantPortal.UseDefaultMultipleIdentitiesMessage,
            input.UseDefaultMessage.ToString());
        return CreateConfiguration(input.UseDefaultMessage, customHtml);
    }

    public async Task<string> RenderAsync(string inboxEmail)
    {
        var configuration = await GetAsync();
        var html = configuration.MessageHtml.Replace(MultipleIdentitiesMessageDefaults.EmailParameter,
            WebUtility.HtmlEncode(inboxEmail), StringComparison.Ordinal);
        return MultipleIdentitiesMessageContent.Sanitize(html);
    }

    private async Task<string?> GetCustomMessageAsync()
    {
        var html = MultipleIdentitiesMessageContent.Sanitize(await settingProvider.GetOrNullAsync(
            SettingsConstants.ApplicantPortal.MultipleIdentitiesMessageHtml));
        return MultipleIdentitiesMessageContent.HasText(html) ? html : null;
    }

    private static MultipleIdentitiesMessageDto CreateConfiguration(bool useDefault, string? customHtml) => new()
    {
        UseDefaultMessage = useDefault,
        MessageHtml = useDefault ? MultipleIdentitiesMessageDefaults.MessageHtml
            : customHtml ?? MultipleIdentitiesMessageDefaults.MessageHtml,
        CustomMessageHtml = customHtml
    };

    private static void ValidateLength(string html)
    {
        if (html.Length > MultipleIdentitiesMessageDefaults.MaxHtmlLength)
        {
            throw ValidationError("Message must not exceed 2,048 characters, including HTML formatting.");
        }
    }

    private static AbpValidationException ValidationError(string message) =>
        new(message, [new ValidationResult(message, [nameof(UpdateMultipleIdentitiesMessageDto.MessageHtml)])]);
}
