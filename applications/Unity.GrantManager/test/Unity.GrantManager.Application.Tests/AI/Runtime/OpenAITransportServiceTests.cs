using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Runtime;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class OpenAITransportServiceTests
{
    [Fact]
    public async Task GenerateSummaryAsync_Should_Include_ProfileReasoningEffortAndVerbosity()
    {
        var handler = new CaptureRequestHandler();
        var resolver = CreateResolver(new Dictionary<string, string?>
        {
            ["Azure:OpenAI:Profiles:Gpt5Mini:ReasoningEffort"] = "minimal",
            ["Azure:OpenAI:Profiles:Gpt5Mini:Verbosity"] = "low"
        });

        var service = new OpenAITransportService(
            new HttpClient(handler),
            resolver,
            NullLogger<OpenAITransportService>.Instance);

        await service.GenerateSummaryAsync("content", "system", 100);

        using var payload = JsonDocument.Parse(handler.RequestContent);
        payload.RootElement.GetProperty("reasoning_effort").GetString().ShouldBe("minimal");
        payload.RootElement.GetProperty("verbosity").GetString().ShouldBe("low");
        payload.RootElement.TryGetProperty("temperature", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateSummaryAsync_Should_Omit_RequestTemperature_When_ProfileDoesNotSupportTemperature()
    {
        var handler = new CaptureRequestHandler();
        var service = new OpenAITransportService(
            new HttpClient(handler),
            CreateResolver(),
            NullLogger<OpenAITransportService>.Instance);

        await service.GenerateSummaryAsync("content", "system", 100, temperature: 0.2);

        using var payload = JsonDocument.Parse(handler.RequestContent);
        payload.RootElement.TryGetProperty("temperature", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateSummaryAsync_Should_ReturnPermanentFailure_When_ProfileOptionIsInvalid()
    {
        var handler = new CaptureRequestHandler();
        var service = new OpenAITransportService(
            new HttpClient(handler),
            CreateResolver(new Dictionary<string, string?>
            {
                ["Azure:OpenAI:Profiles:Gpt5Mini:Verbosity"] = "verbose"
            }),
            NullLogger<OpenAITransportService>.Instance);

        var result = await service.GenerateSummaryAsync("content", "system", 100);

        result.Outcome.ShouldBe(AIOperationOutcome.PermanentFailure);
        handler.SendCount.ShouldBe(0);
    }

    private static OpenAIConfigurationResolver CreateResolver(Dictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Azure:Operations:Defaults:Provider"] = "OpenAI",
            ["Azure:Operations:Defaults:Profile"] = "Gpt5Mini",
            ["Azure:OpenAI:ApiKey"] = "test-key",
            ["Azure:OpenAI:Endpoint"] = "https://example.test",
            ["Azure:OpenAI:Profiles:Gpt5Mini:ApiUrl"] = "/openai/deployments/gpt-5-mini/chat/completions",
            ["Azure:OpenAI:Profiles:Gpt5Mini:MaxTokensParameter"] = "max_completion_tokens"
        };

        if (overrides != null)
        {
            foreach (var item in overrides)
            {
                values[item.Key] = item.Value;
            }
        }

        return new OpenAIConfigurationResolver(new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build());
    }

    private sealed class CaptureRequestHandler : HttpMessageHandler
    {
        public string RequestContent { get; private set; } = string.Empty;

        public int SendCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            RequestContent = request.Content == null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            const string response = """
                {
                  "choices": [
                    {
                      "message": {
                        "content": "generated"
                      },
                      "finish_reason": "stop"
                    }
                  ]
                }
                """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            };
        }
    }
}
