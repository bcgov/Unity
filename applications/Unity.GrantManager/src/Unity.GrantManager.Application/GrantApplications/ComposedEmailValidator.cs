using System;
using System.Linq;
using System.Net.Mail;
using Unity.Modules.Shared.Utils;
using Volo.Abp;

namespace Unity.GrantManager.GrantApplications;

internal static class ComposedEmailValidator
{
    public static void ValidateFields(ComposedEmailDto email)
    {
        if (!IsValidEmailList(email.EmailTo, required: true))
        {
            throw new UserFriendlyException("The email is missing a valid To address.");
        }

        if (!IsValidEmailList(email.EmailCC, required: false))
        {
            throw new UserFriendlyException("The email contains an invalid CC address.");
        }

        if (!IsValidEmailList(email.EmailBCC, required: false))
        {
            throw new UserFriendlyException("The email contains an invalid BCC address.");
        }

        if (string.IsNullOrWhiteSpace(email.EmailFrom))
        {
            throw new UserFriendlyException("The email is missing a From address.");
        }

        if (string.IsNullOrWhiteSpace(email.EmailSubject))
        {
            throw new UserFriendlyException("The email is missing a subject.");
        }

        if (email.EmailSubject.Length > 1023)
        {
            throw new UserFriendlyException("The email subject cannot exceed 1023 characters.");
        }

        if (string.IsNullOrWhiteSpace(email.EmailBody))
        {
            throw new UserFriendlyException("The email is missing a body.");
        }
    }

    public static void ValidateAttachmentSize(long totalAttachmentBytes, double maxAttachmentMb)
    {
        var totalAttachmentMb = totalAttachmentBytes * 0.000001;
        if (totalAttachmentMb > maxAttachmentMb)
        {
            throw new UserFriendlyException(
                $"The total size of all template attachments ({totalAttachmentMb:F2} MB) exceeds the maximum allowed {maxAttachmentMb} MB.");
        }
    }

    private static bool IsValidEmailList(string? value, bool required)
    {
        var addresses = value.ParseEmailList();
        if (addresses is not { Count: > 0 })
        {
            return !required && string.IsNullOrWhiteSpace(value);
        }

        return addresses.All(address =>
            MailAddress.TryCreate(address, out var parsedAddress)
            && parsedAddress != null
            && string.Equals(parsedAddress.Address, address, StringComparison.OrdinalIgnoreCase));
    }
}
