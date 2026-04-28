using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Unity.GrantManager.ApplicantProfile;

namespace Unity.GrantManager.Web.Pages.ApplicantContact;

public class ApplicantContactModalViewModel
{
    [HiddenInput]
    public Guid ApplicantId { get; set; }

    [HiddenInput]
    public Guid Id { get; set; }

    [HiddenInput]
    public bool IsPrimaryInferred { get; set; }

    [DisplayName("ApplicationContact:Type")]
    [Required]
    [StringLength(100)]
    public string Role { get; set; } = string.Empty;

    public List<SelectListItem> RoleOptions { get; set; } = CreateRoleOptions();

    [DisplayName("ApplicationContact:FullName")]
    [Required]
    [MinLength(2)]
    [StringLength(250)]
    public string Name { get; set; } = string.Empty;

    [DisplayName("ApplicationContact:Title")]
    [StringLength(200)]
    public string? Title { get; set; }

    [DisplayName("ApplicationContact:Email")]
    [EmailAddress]
    [StringLength(200)]
    public string? Email { get; set; }

    [DisplayName("ApplicationContact:MobilePhone")]
    [StringLength(50)]
    [RegularExpression(@"^[\+]?[0-9\-\.\(\)\s]*$", ErrorMessage = "Enter a valid phone number")]
    public string? MobilePhoneNumber { get; set; }

    [DisplayName("ApplicationContact:WorkPhone")]
    [StringLength(50)]
    [RegularExpression(@"^[\+]?[0-9\-\.\(\)\s]*$", ErrorMessage = "Enter a valid phone number")]
    public string? WorkPhoneNumber { get; set; }

    [DisplayName("ApplicantContact:SetAsPrimary")]
    public bool IsPrimary { get; set; }

    public void EnsureRoleOptions()
    {
        if (RoleOptions is null || RoleOptions.Count == 0)
        {
            RoleOptions = CreateRoleOptions();
        }
    }

    public static List<SelectListItem> CreateRoleOptions()
    {
        return ApplicantContactRoleOptions.Options
            .Select(option => new SelectListItem { Value = option.Value, Text = option.Label })
            .ToList();
    }
}
