using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AI.Runtime;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class OpenAITransportServiceTests
{
    [Fact]
    public async Task GenerateSummaryAsync_Should_ReturnPermanentFailure_When_ProviderNotSupported()
    {
        var resolver = CreateResolver(new Dictionary<string, string?>
        {
            ["Azure:Operations:Defaults:Provider"] = "UnsupportedProvider"
        });

        var service = new OpenAITransportService(
            resolver,
            new OpenAIChatClientFactory(resolver),
            NullLogger<OpenAITransportService>.Instance);

        var result = await service.GenerateSummaryAsync("content", "system", 100);

        result.Outcome.ShouldBe(AIOperationOutcome.PermanentFailure);
        result.Response?.Content.ShouldContain("Unsupported provider");
    }

    private static OpenAIConfigurationResolver CreateResolver(Dictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Azure:Operations:Defaults:Provider"] = "OpenAI",
            ["Azure:Operations:Defaults:Profile"] = "Gpt5Mini",
            ["Azure:OpenAI:ApiKey"] = "test-key",
            ["Azure:OpenAI:Endpoint"] = "https://example.test",
            ["Azure:OpenAI:Profiles:Gpt5Mini:DeploymentName"] = "gpt-5-mini"
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
}
