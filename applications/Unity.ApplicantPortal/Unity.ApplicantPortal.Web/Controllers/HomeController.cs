using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Unity.ApplicantPortal.Data;
using Unity.ApplicantPortal.Data.Entities;
using Unity.ApplicantPortal.Web.ViewModels;

namespace Unity.ApplicantPortal.Web.Controllers;

[Authorize]
public class HomeController(ILogger<HomeController> logger, AppDbContext dbContext) : Controller
{
    private readonly ILogger<HomeController> _logger = logger;
    private readonly AppDbContext _dbContext = dbContext;

    [HttpGet]
    public IActionResult Index()
    {
        var applicant = _dbContext.Applicants.FirstOrDefault();
        if (applicant == null)
        {
            HomeViewModel model = new();
            return View(model);
        }
        else
        {
            HomeViewModel model = new()
            {
                Applicant = new()
                {
                    ApplicantId = applicant.ApplicantId,
                    OrganizationName = applicant.OrganizationName,
                    PhysicalAddress = applicant.PhysicalAddress,
                    MailingAddress = applicant.MailingAddress,
                    OrganizationNumber = applicant.OrganizationNumber,
                    OrganizationSize = applicant.OrganizationSize,
                    OrganizationBookStatus = applicant.OrganizationBookStatus,
                    OrganizationType = applicant.OrganizationType,
                    OrganizationOperationStartDate = applicant.OrganizationOperationStartDate,
                    OrganizationFiscalYearEnd = applicant.OrganizationFiscalYearEnd,
                    OrganizationSector = applicant.OrganizationSector,
                    OrganizationSubSector = applicant.OrganizationSubSector,
                    OrganizationSocietyNumber = applicant.OrganizationSocietyNumber,
                    OrganizationBusinessLicenseNumber = applicant.OrganizationBusinessLicenseNumber,
                }
            };
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(HomeViewModel model)
    {
        if (ModelState.IsValid)
        {
            var applicant = _dbContext.Applicants.Find(model.Applicant.ApplicantId);
            if (applicant == null)
            {
                //Insert new
                _dbContext.Applicants.Add(new Applicant()
                {
                    OrganizationName = model.Applicant.OrganizationName,
                    PhysicalAddress = model.Applicant.PhysicalAddress,
                    MailingAddress = model.Applicant.MailingAddress,
                    OrganizationNumber = model.Applicant.OrganizationNumber,
                    OrganizationSize = model.Applicant.OrganizationSize,
                    OrganizationBookStatus = model.Applicant.OrganizationBookStatus,
                    OrganizationType = model.Applicant.OrganizationType,
                    OrganizationOperationStartDate = model.Applicant.OrganizationOperationStartDate,
                    OrganizationFiscalYearEnd = model.Applicant.OrganizationFiscalYearEnd,
                    OrganizationSector = model.Applicant.OrganizationSector,
                    OrganizationSubSector = model.Applicant.OrganizationSubSector,
                    OrganizationSocietyNumber = model.Applicant.OrganizationSocietyNumber,
                    OrganizationBusinessLicenseNumber = model.Applicant.OrganizationBusinessLicenseNumber
                });
                _dbContext.SaveChanges();
                TempData["SuccessMessage"] = "Applicant created.";
            }
            else
            {
                applicant.OrganizationName = model.Applicant.OrganizationName;
                applicant.PhysicalAddress = model.Applicant.PhysicalAddress;
                applicant.MailingAddress = model.Applicant.MailingAddress;
                applicant.OrganizationNumber = model.Applicant.OrganizationNumber;
                applicant.OrganizationSize = model.Applicant.OrganizationSize;
                applicant.OrganizationBookStatus = model.Applicant.OrganizationBookStatus;
                applicant.OrganizationType = model.Applicant.OrganizationType;
                applicant.OrganizationOperationStartDate = model.Applicant.OrganizationOperationStartDate;
                applicant.OrganizationFiscalYearEnd = model.Applicant.OrganizationFiscalYearEnd;
                applicant.OrganizationSector = model.Applicant.OrganizationSector;
                applicant.OrganizationSubSector = model.Applicant.OrganizationSubSector;
                applicant.OrganizationSocietyNumber = model.Applicant.OrganizationSocietyNumber;
                applicant.OrganizationBusinessLicenseNumber = model.Applicant.OrganizationBusinessLicenseNumber;
                _dbContext.SaveChanges();
                TempData["SuccessMessage"] = "Applicant updated.";
            }
        }
        return View(model);
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        _logger.LogError("Request error: {RequestId}", Activity.Current?.Id ?? HttpContext.TraceIdentifier);
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
