using Microsoft.Extensions.Configuration;
using Shouldly;
using System.Collections.Generic;
using Unity.AI.Runtime;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class OpenAIConfigurationResolverTests
{
    [Fact]
    public void ResolveApiUrl_Should_CombineEndpointWithLeadingSlashProfilePath()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:Operations:Defaults:Provider"] = "OpenAI",
                ["Azure:Operations:Defaults:Profile"] = "Gpt4oMini",
                ["Azure:OpenAI:Endpoint"] = "https://d837ad-test-recap-webapp.azurewebsites.net",
                ["Azure:OpenAI:Profiles:Gpt4oMini:ApiUrl"] = "/openai/deployments/gpt-4o-mini/chat/completions?api-version=2024-02-01"
            })
            .Build();

        var resolver = new OpenAIConfigurationResolver(configuration);

        resolver.ResolveApiUrl().ShouldBe(
            "https://d837ad-test-recap-webapp.azurewebsites.net/openai/deployments/gpt-4o-mini/chat/completions?api-version=2024-02-01");
    }
}
