using System;
using System.Collections.Generic;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.ApplicantProfile.ProfileData;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantContacts
{
    /// <summary>
    /// View model for the Applicant Contacts widget on the internal Applicant Details page.
    /// Aggregates three contact sources via
    /// <see cref="IApplicantContactQueryService.GetByApplicantIdAsync(Guid)"/>:
    /// applicant-linked (<see cref="Contacts.ContactLink"/>), application contacts, and applicant agent contacts.
    /// Only applicant-linked rows are editable; the primary contact is shown as read-only fields.
    /// </summary>
    public class ApplicantContactsViewModel
    {
        public Guid ApplicantId { get; set; }
        public bool CanEditContact { get; set; }
        public List<ContactInfoItemDto> Contacts { get; set; } = [];
        public ApplicantPrimaryContactDisplayModel? PrimaryContact { get; set; }
        public IReadOnlyList<ApplicantContactRoleOption> RoleOptions { get; set; } = ApplicantContactRoleOptions.Options;
    }

    /// <summary>Read-only display model for the primary contact summary shown above the grid.</summary>
    public class ApplicantPrimaryContactDisplayModel
    {
        public Guid ContactId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string WorkPhone { get; set; } = string.Empty;
        public string MobilePhone { get; set; } = string.Empty;
    }
}
