using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Web.Pages.ApplicationForms;
using Unity.GrantManager.Web.Pages.FormConfiguration;
using Unity.GrantManager.Web.Views.Shared.Components.ApplicationFormConfigWidget;
using Unity.Reporting.Configuration;
using Unity.Reporting.Web.Views.Shared.Components.ReportingConfiguration;
using Unity.Reporting.Web.Views.Shared.Components.ReportingConfigurationViewStatus;
using Volo.Abp.Features;
using Volo.Abp.Settings;
using Xunit;

namespace Unity.GrantManager.Pages;

public class ApplicationFormVisibilityTests
{
    private readonly Guid _formId = GrantManagerTestData.ApplicationForm1_Id;
    private readonly IApplicationFormAppService _forms = Substitute.For<IApplicationFormAppService>();

    public ApplicationFormVisibilityTests()
    {
        _forms.GetAsync(_formId).Returns(new ApplicationFormDto { Id = _formId, IsHiddenFromList = true });
    }

    [Fact]
    public async Task Should_ReturnNotFound_WhenOpeningHiddenFormConfiguration()
    {
        var versions = Substitute.For<IApplicationFormVersionAppService>();
        var page = new MappingModel(
            _forms,
            versions,
            Substitute.For<IApplicationFormVersionMappingReadService>(),
            Substitute.For<IFeatureChecker>(),
            Substitute.For<ISettingProvider>())
        {
            ApplicationId = _formId
        };

        (await page.OnGetAsync()).ShouldBeOfType<NotFoundResult>();
        await versions.DidNotReceive().CreateAsync(Arg.Any<CreateUpdateApplicationFormVersionDto>());
    }

    [Fact]
    public async Task Should_ReturnNotFound_WhenOpeningOrSubmittingHiddenFormEdit()
    {
        var page = new UpdateModalModel(_forms, Substitute.For<IIntakeAppService>()) { Id = _formId };

        (await page.OnGetAsync()).ShouldBeOfType<NotFoundResult>();
        (await page.OnPostAsync()).ShouldBeOfType<NotFoundResult>();

        await _forms.DidNotReceive().UpdateAsync(_formId, Arg.Any<Unity.GrantManager.Forms.CreateUpdateApplicationFormDto>());
    }

    [Fact]
    public async Task Should_ReturnNotFound_WhenOpeningHiddenFormNotifications()
    {
        var page = new NotificationsModel(NullLogger<NotificationsModel>.Instance, _forms)
        {
            FormId = _formId.ToString()
        };

        (await page.OnGetAsync(_formId.ToString())).ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Should_ReloadPersistedVisibility_WhenRefreshingFormConfiguration()
    {
        var controller = new ApplicationFormConfigWidgetController(_forms);
        var postedForm = new ApplicationFormDto { Id = _formId, IsHiddenFromList = false };

        (await controller.Refresh("Other", postedForm)).ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Should_ReturnNotFound_WhenRefreshingHiddenFormReportingConfiguration()
    {
        var controller = new ReportingConfigurationController(
            Substitute.For<IReportMappingService>(), _forms, Substitute.For<IApplicationFormVersionAppService>());

        (await controller.Refresh(_formId)).ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Should_RejectReportingRefresh_WhenVersionBelongsToAnotherForm()
    {
        _forms.GetAsync(_formId).Returns(new ApplicationFormDto { Id = _formId });
        var versions = Substitute.For<IApplicationFormVersionAppService>();
        var versionId = GrantManagerTestData.Application1_Id;
        versions.GetAsync(versionId).Returns(new ApplicationFormVersionDto
        {
            Id = versionId,
            ApplicationFormId = GrantManagerTestData.Application2_Id
        });
        var controller = new ReportingConfigurationController(Substitute.For<IReportMappingService>(), _forms, versions);

        (await controller.Refresh(_formId, versionId)).ShouldBeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData("formversion_consolidated")]
    [InlineData("worksheet_consolidated")]
    public async Task Should_ReturnNotFound_WhenRefreshingHiddenFormReportingStatus(string provider)
    {
        var controller = new ReportingConfigurationViewStatusController(
            Substitute.For<IReportMappingService>(), _forms, Substitute.For<IApplicationFormVersionAppService>());

        (await controller.Refresh(_formId, provider)).ShouldBeOfType<NotFoundResult>();
    }
}
