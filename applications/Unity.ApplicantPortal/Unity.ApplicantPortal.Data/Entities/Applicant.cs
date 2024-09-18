using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Unity.ApplicantPortal.Data.Entities;

public class Applicant
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ApplicantId { get; set; }

    #region Organization details
    [Required]
    [MaxLength(500)]
    public string OrganizationName { get; set; } = string.Empty;

    [Required]
    [MaxLength(25)]
    public string OrganizationNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string OrganizationSize { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string OrganizationBookStatus { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string OrganizationType { get; set; } = string.Empty;

    [Required]
    public DateOnly OrganizationOperationStartDate { get; set; }

    [Required]
    public DateOnly OrganizationFiscalYearEnd { get; set; }

    [MaxLength(100)]
    public string? OrganizationSector { get; set; }

    [MaxLength(100)]
    public string? OrganizationSubSector { get; set; }

    [MaxLength(25)]
    public string? OrganizationSocietyNumber { get; set; }

    [MaxLength(25)]
    public string? OrganizationBusinessLicenseNumber { get; set; }
    #endregion

    #region Addresses
    [Required]
    [MaxLength(500)]
    public string PhysicalAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string MailingAddress { get; set; } = string.Empty;
    #endregion
}
