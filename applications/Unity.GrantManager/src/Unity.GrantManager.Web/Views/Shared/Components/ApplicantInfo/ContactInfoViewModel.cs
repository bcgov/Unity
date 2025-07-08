using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo;

public class ContactInfoViewModel
{
    public Guid? ApplicantAgentId { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactFullName")]
    [MaxLength(600, ErrorMessage = "Must be a maximum of 600 characters")]
    public string? Name { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactTitle")]
    public string? Title { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactEmail")]
    [DataType(DataType.EmailAddress, ErrorMessage = "Provided email is not valid")]
    public string? Email { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactBusinessPhone")]
    [DataType(DataType.PhoneNumber, ErrorMessage = "Invalid Phone Number")]
    [RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
    public string? Phone { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactCellPhone")]
    [DataType(DataType.PhoneNumber, ErrorMessage = "Invalid Phone Number")]
    [RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
    public string? Phone2 { get; set; }
}

