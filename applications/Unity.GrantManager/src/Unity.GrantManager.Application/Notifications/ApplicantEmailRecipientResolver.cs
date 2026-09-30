using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Unity.GrantManager.ApplicantProfile.ProfileData;

namespace Unity.GrantManager.Notifications;

public static class ApplicantEmailRecipientResolver
{
    public static string Resolve(string? identifiers, IEnumerable<ContactInfoItemDto> contacts)
    {
        var contactList = contacts.ToList();
        var primary = contactList.Where(c => c.IsPrimary).OrderByDescending(c => c.CreationTime).FirstOrDefault();
        var addresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var identifier in (identifiers ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(identifier, "ApplicationContact", StringComparison.OrdinalIgnoreCase))
            {
                AddAddress(primary?.Email);
            }
            else if (string.Equals(identifier, "SigningAuthority", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var contact in contactList.Where(c => IsSigningAuthority(c.Role)))
                {
                    AddAddress(contact.Email);
                }
            }
            else
            {
                AddAddress(identifier);
            }
        }
        return string.Join("; ", addresses);

        void AddAddress(string? address)
        {
            if (!string.IsNullOrWhiteSpace(address) && MailAddress.TryCreate(address.Trim(), out var parsed))
            {
                addresses.Add(parsed.Address);
            }
        }
    }

    private static bool IsSigningAuthority(string? role)
    {
        var normalized = (role ?? string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty);
        return normalized.Equals("SigningAuthority", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("AdditionalSigningAuthority", StringComparison.OrdinalIgnoreCase);
    }
}
