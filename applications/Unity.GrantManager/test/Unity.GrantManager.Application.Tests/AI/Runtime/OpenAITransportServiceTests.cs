using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System.Threading.Tasks;
using Unity.AI.Runtime;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class OpenAITransportServiceTests
{
    [Fact]
    public async Task GenerateSummaryAsync_Should_ReturnPermanentFailure_When_ProviderNotSupported()
    {
        var service = new OpenAITransportService(
            new OpenAIChatClientFactory(),
            NullLogger<OpenAITransportService>.Instance);

        var settings = new OpenAIOperationSettings(
            "UnsupportedProvider",
            "Gpt5Mini",
            "test-key",
            new System.Uri("https://example.test"),
            "gpt-5-mini",
            true,
            null,
            100,
            "v1");

        var result = await service.GenerateSummaryAsync("content", "system", settings, 100);

        result.Outcome.ShouldBe(AIOperationOutcome.PermanentFailure);
        result.Response?.Content.ShouldContain("Unsupported provider");
    }
}
