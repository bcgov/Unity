using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Unity.AI.Runtime.Execution;

/// <summary>
/// Azure OpenAI API versions as set per model in AI Setup. Any version Azure publishes can be
/// used (2024-10-21, 2025-04-01-preview, ...): UGM sends it itself rather than being limited to
/// the versions its SDK knows, and Test shows whether Azure accepts it for the deployment.
/// </summary>
public static partial class AzureOpenAIApiVersions
{
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}(-preview)?$")]
    private static partial Regex Format();

    /// <summary>True for a well-formed API version such as 2024-10-21 or 2025-04-01-preview.</summary>
    public static bool IsValid([NotNullWhen(true)] string? apiVersion)
    {
        return !string.IsNullOrWhiteSpace(apiVersion) && Format().IsMatch(apiVersion.Trim());
    }
}

/// <summary>
/// Sets ?api-version= on every Azure OpenAI request to the model's version from AI Setup,
/// replacing the one the SDK adds, so the call is
/// .../openai/deployments/{deployment}/...?api-version={model's version}.
/// </summary>
public sealed class AzureOpenAIApiVersionPolicy(string apiVersion) : PipelinePolicy
{
    public string ApiVersion { get; } = apiVersion;

    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        message.Request.Uri = WithApiVersion(message.Request.Uri!, ApiVersion);
        ProcessNext(message, pipeline, currentIndex);
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        message.Request.Uri = WithApiVersion(message.Request.Uri!, ApiVersion);
        await ProcessNextAsync(message, pipeline, currentIndex);
    }

    /// <summary>The URI with its api-version query parameter set to the given value (other parameters kept).</summary>
    public static Uri WithApiVersion(Uri uri, string apiVersion)
    {
        var builder = new UriBuilder(uri);
        var parameters = builder.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(parameter => !parameter.StartsWith("api-version=", StringComparison.OrdinalIgnoreCase))
            .Append("api-version=" + Uri.EscapeDataString(apiVersion));
        builder.Query = string.Join("&", parameters);
        return builder.Uri;
    }
}
