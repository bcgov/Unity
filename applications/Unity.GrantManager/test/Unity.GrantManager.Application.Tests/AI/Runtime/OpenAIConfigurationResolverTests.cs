using Microsoft.Extensions.Configuration;
using Shouldly;
using System;
using System.Collections.Generic;
using Unity.AI.Runtime;
using Xunit;

namespace Unity.GrantManager.AI.Runtime;

public class OpenAIConfigurationResolverTests
{
    [Fact]
    public void ResolveProviderName_Should_Throw_When_DefaultProvider_Is_Missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var resolver = new OpenAIConfigurationResolver(configuration);

        var ex = Should.Throw<InvalidOperationException>(() => resolver.ResolveProviderName());
        ex.Message.ShouldContain("Azure:Operations:Defaults:Provider");
    }

    [Fact]
    public void ResolveCompletionTokens_Should_Throw_When_Operation_And_Default_Are_Missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var resolver = new OpenAIConfigurationResolver(configuration);

        var ex = Should.Throw<InvalidOperationException>(() => resolver.ResolveCompletionTokens("ApplicationAnalysis"));
        ex.Message.ShouldContain("ApplicationAnalysis");
    }

    [Fact]
    public void ResolvePromptVersion_Should_Use_Operation_Override_Before_Default()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:Operations:Defaults:PromptVersion"] = "v0",
                ["Azure:Operations:ApplicationAnalysis:PromptVersion"] = "v1"
            })
            .Build();

        var resolver = new OpenAIConfigurationResolver(configuration);

        resolver.ResolvePromptVersion("ApplicationAnalysis").ShouldBe("v1");
    }

    [Fact]
    public void ResolvePromptVersion_Should_Throw_When_Default_Is_Missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var resolver = new OpenAIConfigurationResolver(configuration);

        var ex = Should.Throw<InvalidOperationException>(() => resolver.ResolvePromptVersion("ApplicationAnalysis"));
        ex.Message.ShouldContain("Azure:Operations:Defaults:PromptVersion");
    }

    [Fact]
    public void ResolveApiKey_Should_Throw_When_Configured_Key_Is_Missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:Operations:Defaults:Provider"] = "OpenAI"
            })
            .Build();

        var resolver = new OpenAIConfigurationResolver(configuration);

        var ex = Should.Throw<InvalidOperationException>(() => resolver.ResolveApiKey());
        ex.Message.ShouldContain("Azure:OpenAI:ApiKey");
    }

    [Fact]
    public void ResolveEndpoint_Should_Return_Configured_Provider_Endpoint()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:Operations:Defaults:Provider"] = "OpenAI",
                ["Azure:OpenAI:Endpoint"] = "https://example.test"
            })
            .Build();

        var resolver = new OpenAIConfigurationResolver(configuration);

        resolver.ResolveEndpoint().ShouldBe(new Uri("https://example.test"));
    }

    [Fact]
    public void ResolveDeploymentName_Should_Return_Profile_DeploymentName()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:Operations:Defaults:Provider"] = "OpenAI",
                ["Azure:Operations:Defaults:Profile"] = "Gpt5Mini",
                ["Azure:OpenAI:Profiles:Gpt5Mini:DeploymentName"] = "gpt-5-mini"
            })
            .Build();

        var resolver = new OpenAIConfigurationResolver(configuration);

        resolver.ResolveDeploymentName().ShouldBe("gpt-5-mini");
    }

    [Fact]
    public void ResolveDeploymentName_Should_Use_Operation_Profile_Override()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:Operations:Defaults:Provider"] = "OpenAI",
                ["Azure:Operations:Defaults:Profile"] = "Gpt5Mini",
                ["Azure:Operations:ApplicationAnalysis:Profile"] = "Gpt4oMini",
                ["Azure:OpenAI:Profiles:Gpt5Mini:DeploymentName"] = "gpt-5-mini",
                ["Azure:OpenAI:Profiles:Gpt4oMini:DeploymentName"] = "gpt-4o-mini"
            })
            .Build();

        var resolver = new OpenAIConfigurationResolver(configuration);

        resolver.ResolveDeploymentName("ApplicationAnalysis").ShouldBe("gpt-4o-mini");
    }

    [Fact]
    public void ResolveDeploymentName_Should_Throw_When_Profile_DeploymentName_Is_Missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:Operations:Defaults:Provider"] = "OpenAI",
                ["Azure:Operations:Defaults:Profile"] = "Gpt5Mini"
            })
            .Build();

        var resolver = new OpenAIConfigurationResolver(configuration);

        var ex = Should.Throw<InvalidOperationException>(() => resolver.ResolveDeploymentName());
        ex.Message.ShouldContain("Azure:OpenAI:Profiles:Gpt5Mini:DeploymentName");
    }

    [Fact]
    public void ResolveConfiguredTemperature_Should_Return_Profile_Temperature()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:Operations:Defaults:Provider"] = "OpenAI",
                ["Azure:Operations:Defaults:Profile"] = "Gpt4oMini",
                ["Azure:OpenAI:Profiles:Gpt4oMini:Temperature"] = "0.3"
            })
            .Build();

        var resolver = new OpenAIConfigurationResolver(configuration);

        resolver.ResolveConfiguredTemperature().ShouldBe(0.3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-number")]
    public void ResolveConfiguredTemperature_Should_Return_Null_When_Profile_Temperature_Is_Missing_Or_Invalid(string? temperature)
    {
        var values = new Dictionary<string, string?>
        {
            ["Azure:Operations:Defaults:Provider"] = "OpenAI",
            ["Azure:Operations:Defaults:Profile"] = "Gpt4oMini"
        };

        if (temperature != null)
        {
            values["Azure:OpenAI:Profiles:Gpt4oMini:Temperature"] = temperature;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var resolver = new OpenAIConfigurationResolver(configuration);

        resolver.ResolveConfiguredTemperature().ShouldBeNull();
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void ResolveMaxOutputTokenCountSupported_Should_Use_Profile_Setting_Or_Default_True(
        string? configuredValue,
        bool expected)
    {
        var values = new Dictionary<string, string?>
        {
            ["Azure:Operations:Defaults:Provider"] = "OpenAI",
            ["Azure:Operations:Defaults:Profile"] = "Gpt5Mini"
        };

        if (configuredValue != null)
        {
            values["Azure:OpenAI:Profiles:Gpt5Mini:MaxOutputTokenCountSupported"] = configuredValue;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var resolver = new OpenAIConfigurationResolver(configuration);

        resolver.ResolveMaxOutputTokenCountSupported().ShouldBe(expected);
    }
}
