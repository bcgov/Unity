using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Contacts;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.GrantsPortal.Notifications;

public record ApplicantUpdateField(string Name, string? Value, string? PreviousValue = null);

public record ApplicantUpdateDetails(string UpdateType, IReadOnlyList<ApplicantUpdateField> Fields, bool IsComparison = false)
{
    // Capture values, not the tracked entity, before OrganizationEditHandler mutates it.
    public static IReadOnlyList<ApplicantUpdateField> CaptureApplicantInformation(Applicant applicant) =>
    [
        new("Organization name", applicant.OrgName),
        new("Organization type", applicant.OrganizationType),
        new("Organization number", applicant.OrgNumber),
        new("Organization status", applicant.OrgStatus),
        new("Nonregistered organization name", applicant.NonRegOrgName),
        new("Fiscal month", applicant.FiscalMonth),
        new("Fiscal day", applicant.FiscalDay?.ToString(CultureInfo.InvariantCulture)),
        new("Approximate number of employees", applicant.ApproxNumberOfEmployees)
    ];

    public static ApplicantUpdateDetails ApplicantInformation(
        IReadOnlyList<ApplicantUpdateField> previous, Applicant applicant)
    {
        var previousValues = previous.ToDictionary(field => field.Name, field => field.Value);
        var changes = CaptureApplicantInformation(applicant)
            .Where(field => !string.Equals(previousValues[field.Name], field.Value, StringComparison.Ordinal))
            .Select(field => field with { PreviousValue = previousValues[field.Name] })
            .ToArray();
        return new("Applicant Information", changes, IsComparison: true);
    }

    public static ApplicantUpdateDetails ContactCreated(Contact contact, ContactLink link, string? contactType) => new("Contact",
    [
        new("Contact name", contact.Name),
        new("Title", contact.Title),
        new("Contact type", contactType),
        new("Role", link.Role),
        new("Email address", contact.Email),
        new("Home phone", contact.HomePhoneNumber),
        new("Mobile phone", contact.MobilePhoneNumber),
        new("Work phone", contact.WorkPhoneNumber),
        new("Work phone extension", contact.WorkPhoneExtension),
        new("Primary contact", link.IsPrimary ? "Yes" : "No")
    ]);

    public static ApplicantUpdateDetails AddressCreated(ApplicantAddress address, bool isPrimary) => new("Address",
    [
        new("Address type", address.AddressType switch
        {
            AddressType.PhysicalAddress => "Physical address",
            AddressType.MailingAddress => "Mailing address",
            AddressType.BusinessAddress => "Business address",
            _ => address.AddressType.ToString()
        }),
        new("Unit", address.Unit),
        new("Street", address.Street),
        new("Street line 2", address.Street2),
        new("City", address.City),
        new("Province", address.Province),
        new("Postal code", address.Postal),
        new("Country", address.Country),
        new("Primary address", isPrimary ? "Yes" : "No")
    ]);
}
