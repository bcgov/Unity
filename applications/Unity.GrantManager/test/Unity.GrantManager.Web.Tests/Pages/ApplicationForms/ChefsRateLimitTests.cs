using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Integrations;
using Unity.GrantManager.Integrations.Chefs;
using Unity.Modules.Shared.Http;
using Unity.Reporting.Configuration;
using Unity.Reporting.Web.Views.Shared.Components.ReportingConfiguration;
using Volo.Abp.Security.Encryption;
using Xunit;

namespace Unity.GrantManager.Web.Pages.ApplicationForms;

public class ChefsRateLimitTests
{
    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, "Too many requests, please try again later.")]
    [InlineData(HttpStatusCode.Unauthorized, "{\"message\":\"Unauthorized\"}")]
    public async Task FormsApi_Should_Preserve_Http_Error_Instead_Of_Parsing_Error_Body(
        HttpStatusCode status, string content)
    {
        using var response = new HttpResponseMessage(status) { Content = new StringContent(content) };
        var service = CreateFormsApi(response);

        var exception = await Should.ThrowAsync<HttpRequestException>(() =>
            service.GetForm(Guid.NewGuid(), Guid.NewGuid().ToString(), "encrypted-test-key"));

        exception.StatusCode.ShouldBe(status);
    }

    [Fact]
    public async Task FormsApi_Should_Still_Parse_Successful_Json()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"name\":\"Test form\"}")
        };
        var service = CreateFormsApi(response);

        var result = await service.GetForm(Guid.NewGuid(), Guid.NewGuid().ToString(), "encrypted-test-key");

        result["name"]?.ToString().ShouldBe("Test form");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reporting_Should_Render_RateLimit_Warning_Without_Failing_Page(bool hasSavedConfiguration)
    {
        var (component, mappings, formId) = CreateReportingComponent();
        mappings.ExistsAsync(formId, Providers.FormVersionConsolidated).Returns(hasSavedConfiguration);
        mappings.GetFieldsMetadataAsync(formId, Providers.FormVersionConsolidated)
            .ThrowsAsync(new HttpRequestException("CHEFS rate limited", null, HttpStatusCode.TooManyRequests));
        mappings.GetByCorrelationAsync(formId, Providers.FormVersionConsolidated)
            .ThrowsAsync(new HttpRequestException("CHEFS rate limited", null, HttpStatusCode.TooManyRequests));

        var result = await component.InvokeAsync(formId);

        result.ShouldBeOfType<ViewViewComponentResult>().ViewName.ShouldBe("RateLimited");
    }

    [Fact]
    public async Task Reporting_Should_Not_Hide_Other_Http_Errors()
    {
        var (component, mappings, formId) = CreateReportingComponent();
        mappings.GetFieldsMetadataAsync(formId, Providers.FormVersionConsolidated)
            .ThrowsAsync(new HttpRequestException("CHEFS unauthorized", null, HttpStatusCode.Unauthorized));

        var exception = await Should.ThrowAsync<HttpRequestException>(() => component.InvokeAsync(formId));

        exception.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static FormsApiService CreateFormsApi(HttpResponseMessage response)
    {
        var endpoints = Substitute.For<IEndpointManagementAppService>();
        endpoints.GetChefsApiBaseUrlAsync().Returns("https://chefs.example/app/api/v1");
        var encryption = Substitute.For<IStringEncryptionService>();
        encryption.Decrypt("encrypted-test-key").Returns("test-key");
        var http = Substitute.For<IResilientHttpRequest>();
        http.HttpAsync(HttpMethod.Get, string.Empty).ReturnsForAnyArgs(response);

        return new FormsApiService(endpoints, Substitute.For<IApplicationFormRepository>(),
            encryption, http, NullLogger<FormsApiService>.Instance);
    }

    private static (ReportingConfigurationViewComponent Component, IReportMappingService Mappings, Guid FormId)
        CreateReportingComponent()
    {
        var formId = Guid.NewGuid();
        var forms = Substitute.For<IApplicationFormAppService>();
        forms.GetVersionsAsync(formId).Returns(new List<ApplicationFormVersionDto>());
        var mappings = Substitute.For<IReportMappingService>();
        var component = new ReportingConfigurationViewComponent(forms, mappings,
            NullLogger<ReportingConfigurationViewComponent>.Instance)
        {
            ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext
                {
                    ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                }
            }
        };
        return (component, mappings, formId);
    }
}
