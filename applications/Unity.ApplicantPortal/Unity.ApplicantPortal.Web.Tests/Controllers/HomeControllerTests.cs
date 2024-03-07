using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Unity.ApplicantPortal.Web.ViewModels;

namespace Unity.ApplicantPortal.Web.Controllers.Tests;

[TestClass()]
public class HomeControllerTests : TestBase
{
    #region Initialization
    private readonly HomeController _controller;

    public HomeControllerTests() : base()
    {
        var logger = InitializeLogger<HomeController>();
        var httpContext = new DefaultHttpContext();
        var mockTempDataProvider = new Mock<ITempDataProvider>();
        _controller = new(logger, _dbContext)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, mockTempDataProvider.Object)
        };
    }

    [TestInitialize]
    public void SeedData()
    {
        _dbContext.Applicants.Add(new Data.Entities.Applicant()
        {
            ApplicantId = 1,
            OrganizationName = "OrgName",
            PhysicalAddress = "Address 1",
            MailingAddress = "Address 2",
            OrganizationNumber = "abc123",
            OrganizationSize = "Medium",
            OrganizationType = "Non-Profit",
            OrganizationOperationStartDate = new DateOnly(2024, 3, 6),
            OrganizationFiscalYearEnd = new DateOnly(2024, 3, 31),
        });
        _dbContext.SaveChanges();
    }
    #endregion

    [TestMethod()]
    public void IndexTest()
    {
        var result = _controller.Index();
        Assert.IsNotNull(result);
        Assert.IsTrue(result is ViewResult);
        var viewModel = (result as ViewResult)!.Model;
        Assert.IsTrue(viewModel is HomeViewModel);
        Assert.IsTrue((viewModel as HomeViewModel)!.Applicant.OrganizationName == "OrgName");
    }

    [TestMethod]
    public void UpdateTest()
    {
        HomeViewModel data = new()
        {
            Applicant = new()
            {
                ApplicantId = 1,
                OrganizationName = "OrgNameUpdateTest",
                PhysicalAddress = "Address 123",
                MailingAddress = "Address 1234",
                OrganizationNumber = "abc123",
                OrganizationSize = "Medium",
                OrganizationType = "Non-Profit",
                OrganizationOperationStartDate = new DateOnly(2024, 3, 6),
                OrganizationFiscalYearEnd = new DateOnly(2024, 3, 31),
            }
        };
        var result = _controller.Index(data);
        Assert.IsNotNull(result);
        Assert.IsTrue(result is ViewResult);
        var dbApplicant = _dbContext.Applicants.Find(1);
        Assert.IsNotNull(dbApplicant);
        Assert.AreEqual("OrgNameUpdateTest", dbApplicant.OrganizationName);
    }

    [TestMethod()]
    public void ErrorTest()
    {
        var result = _controller.Error();
        Assert.IsNotNull(result);
        Assert.IsTrue(result is ViewResult);
        var viewModel = (result as ViewResult)!.Model;
        Assert.IsTrue(viewModel is ErrorViewModel);
    }
}
