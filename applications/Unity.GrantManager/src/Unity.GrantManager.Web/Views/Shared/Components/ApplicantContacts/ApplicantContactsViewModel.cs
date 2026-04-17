using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantContacts
{
    public class ApplicantContactsViewModel
    {
        public Guid ApplicantId { get; set; }
        public bool CanEditContact { get; set; }
        public bool CanSave => CanEditContact && PrimaryContact.IsEditable;
        public ApplicantPrimaryContactViewModel PrimaryContact { get; set; } = new();
        public List<ApplicantContactItemDto> Contacts { get; set; } = new();
    }

    public class ApplicantPrimaryContactViewModel
    {
        public Guid Id { get; set; }
        public string Source { get; set; } = string.Empty;
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;
        [Display(Name = "Business Phone")]
        public string BusinessPhone { get; set; } = string.Empty;
        [Display(Name = "Cell Phone")]
        public string CellPhone { get; set; } = string.Empty;
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
        public bool IsEditable => Id != Guid.Empty;
    }

    public class ApplicantContactItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public DateTime CreationTime { get; set; }
        public string ReferenceNo { get; set; } = string.Empty;
        public Guid? ApplicationId { get; set; }
    }
}
