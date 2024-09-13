using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Unity.ApplicantPortal.Web.ViewModels;

public class ApplicantViewModel
{
    public int ApplicantId { get; set; }

    #region Organization details
    [Required(ErrorMessage = "Please enter the organization's name.")]
    [JsonRequired]
    [DisplayName("Organization")]
    [MaxLength(500)]
    public string? OrganizationName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the organization number.")]
    [DisplayName("Org #")]
    [MaxLength(25)]
    public string OrganizationNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select the organization's size.")]
    [DisplayName("Size")]
    [MaxLength(50)]
    public string OrganizationSize { get; set; } = "Medium";

    public List<string> OrganizationSizes
    {
        get
        {
            //Need to define these better; this is a guess based on the UI screenshot
            return ["Small", "Medium", "Large"];
        }
    }

    [Required(ErrorMessage = "Please select the organization's book status.")]
    [DisplayName("Book Status")]
    [MaxLength(50)]
    public string OrganizationBookStatus { get; set; } = "Good";

    public List<string> OrganizationBookStatuses
    {
        get
        {
            //Need to define these better; this is a guess based on the UI screenshot
            return ["Good", "At Risk"];
        }
    }

    [Required(ErrorMessage = "Please select the organization's type.")]
    [DisplayName("Type")]
    [MaxLength(50)]
    public string OrganizationType { get; set; } = "Non-Profit";

    public List<string> OrganizationTypes
    {
        get
        {
            //Need to define these better; this is a guess based on the UI screenshot
            return ["Sole Proprietor", "Corporation", "Non-Profit"];
        }
    }

    [Required(ErrorMessage = "Please enter the date when the organization started operating.")]
    [DisplayName("Start Date of Operation")]
    [DataType(DataType.Date)]
    public DateOnly OrganizationOperationStartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    [Required(ErrorMessage = "Please enter the organization's next fiscal year end date.")]
    [DisplayName("Fiscal Year End")]
    [DataType(DataType.Date)]
    public DateOnly OrganizationFiscalYearEnd { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    [DisplayName("Sector")]
    [MaxLength(100)]
    public string? OrganizationSector { get; set; }

    [DisplayName("Sub-Sector")]
    [MaxLength(100)]
    public string? OrganizationSubSector { get; set; }

    [DisplayName("BC Society Number")]
    [MaxLength(25)]
    public string? OrganizationSocietyNumber { get; set; }

    [DisplayName("Business License Number")]
    [MaxLength(25)]
    public string? OrganizationBusinessLicenseNumber { get; set; }
    #endregion

    #region Addresses
    [Required(ErrorMessage = "Please enter the organization's physical address.")]
    [DisplayName("Physical Address")]
    [MaxLength(500)]
    public string PhysicalAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the organization's mailing address.")]
    [DisplayName("Mailing Address")]
    [MaxLength(500)]
    public string MailingAddress { get; set; } = string.Empty;
    #endregion
}
