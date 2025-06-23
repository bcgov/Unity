using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo;

public class SigningAuthorityViewModel
{
    [Display(Name = "ApplicantInfoView:ApplicantInfo.SigningAuthorityFullName")]
    [MaxLength(600, ErrorMessage = "Must be a maximum of 6 characters")]
    public string? SigningAuthorityFullName { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.SigningAuthorityTitle")]
    public string? SigningAuthorityTitle { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.SigningAuthorityEmail")]
    [DataType(DataType.EmailAddress, ErrorMessage = "Provided email is not valid")]
    public string? SigningAuthorityEmail { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.SigningAuthorityBusinessPhone")]
    [DataType(DataType.PhoneNumber, ErrorMessage = "Invalid Phone Number")]
    [RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
    public string? SigningAuthorityBusinessPhone { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.SigningAuthorityCellPhone")]
    [DataType(DataType.PhoneNumber, ErrorMessage = "Invalid Phone Number")]
    [RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
    public string? SigningAuthorityCellPhone { get; set; }
}

