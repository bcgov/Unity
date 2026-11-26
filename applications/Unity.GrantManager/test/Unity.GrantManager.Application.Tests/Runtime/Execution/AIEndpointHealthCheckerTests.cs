using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Runtime.Execution;
using Unity.AI.Setup;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class AIEndpointHealthCheckerTests
{
    [Fact]
    public async Task Should_Report_The_Proxys_Healthz_Text()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("healthy - Azure proxy responding, configured for d837ad-dev-econ-llm-east.openai.azure.com")
        });

        var status = await CreateChecker("http://unity-azure-llm-proxy/", handler).CheckAsync();

        status.Status.ShouldBe(AIEndpointStatusDto.Healthy);
        status.Message.ShouldContain("d837ad-dev-econ-llm-east");
        handler.RequestedUri!.ToString().ShouldBe("http://unity-azure-llm-proxy/healthz");
        handler.SentApiKey.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Report_Reachable_Without_Health_Check_When_Endpoint_Has_No_Healthz()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var status = await CreateChecker("https://example.openai.azure.com/", handler).CheckAsync();

        status.Status.ShouldBe(AIEndpointStatusDto.NoHealthCheck);
        status.HttpStatusCode.ShouldBe(404);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Should_Report_Unhealthy_With_The_Proxys_Message_When_Healthz_Fails(HttpStatusCode code)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(code)
        {
            Content = new StringContent("unhealthy - Azure proxy not responding")
        });

        var status = await CreateChecker("http://unity-azure-llm-proxy/", handler).CheckAsync();

        status.Status.ShouldBe(AIEndpointStatusDto.Unhealthy);
        status.HttpStatusCode.ShouldBe((int)code);
        status.Message.ShouldContain("Azure proxy not responding");
    }

    [Fact]
    public async Task Should_Report_Unreachable_When_The_Call_Fails()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("Name or service not known"));

        var status = await CreateChecker("http://unity-azure-llm-proxy/", handler).CheckAsync();

        status.Status.ShouldBe(AIEndpointStatusDto.Unreachable);
        status.Message.ShouldContain("Name or service not known");
    }

    [Fact]
    public async Task Should_Report_Not_Configured_Without_Calling_Anything()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var status = await CreateChecker(null, handler).CheckAsync();

        status.Status.ShouldBe(AIEndpointStatusDto.NotConfigured);
        handler.RequestedUri.ShouldBeNull();
    }

    private static AIEndpointHealthChecker CreateChecker(string? endpoint, HttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Azure:OpenAI:Endpoint"] = endpoint })
            .Build();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler, disposeHandler: false));
        return new AIEndpointHealthChecker(configuration, factory);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public Uri? RequestedUri { get; private set; }
        public bool SentApiKey { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUri = request.RequestUri;
            SentApiKey = request.Headers.Contains("api-key");
            return Task.FromResult(respond(request));
        }
    }
}
