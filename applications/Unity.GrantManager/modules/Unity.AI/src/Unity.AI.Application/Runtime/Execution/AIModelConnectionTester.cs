using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Setup;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime.Execution;

/// <summary>
/// Makes one small chat call to a model's Azure deployment with the configured endpoint and key,
/// so a missing or retired deployment shows up before an operation uses it.
/// </summary>
public class AIModelConnectionTester(
    OpenAITransportService transportService,
    IConfiguration configuration) : ITransientDependency
{
    public const string EndpointNotConfigured = "EndpointNotConfigured";
    public const string ApiKeyNotConfigured = "ApiKeyNotConfigured";

    public virtual async Task<AIModelTestResultDto> TestAsync(AIModel model, CancellationToken cancellationToken = default)
    {
        var endpointValue = configuration[$"Azure:{model.Provider}:Endpoint"]?.Trim();
        if (string.IsNullOrWhiteSpace(endpointValue) || !Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint))
        {
            return Failure(EndpointNotConfigured);
        }

        var apiKey = configuration[$"Azure:{model.Provider}:ApiKey"]?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Failure(ApiKeyNotConfigured);
        }

        var settings = new OpenAIOperationSettings(
            model.Provider,
            model.Name,
            apiKey,
            endpoint,
            model.Name,
            "test");

        var stopwatch = Stopwatch.StartNew();
        var result = await transportService.GenerateSummaryAsync(
            "Reply with the single word OK.",
            "You are a connectivity check for Unity AI Setup.",
            settings,
            cancellationToken);
        stopwatch.Stop();

        var statusCode = result.Response.HttpStatusCode;

        // Any 200 means the deployment answered, even with empty output.
        var answered = result.Outcome == AIOperationOutcome.Success
            || (result.Outcome == AIOperationOutcome.InvalidOutput && statusCode == 200);

        return new AIModelTestResultDto
        {
            Success = answered,
            HttpStatusCode = statusCode,
            ErrorCode = answered ? null : ExtractAzureErrorCode(result.Response.RawResponse),
            LatencyMilliseconds = stopwatch.ElapsedMilliseconds,
            Message = answered ? string.Empty : ExtractAzureErrorMessage(result.Response)
        };
    }

    private static AIModelTestResultDto Failure(string errorCode)
    {
        return new AIModelTestResultDto { Success = false, ErrorCode = errorCode };
    }

    private static string? ExtractAzureErrorCode(string? rawResponse)
    {
        return TryReadError(rawResponse, "code");
    }

    private static string ExtractAzureErrorMessage(AIProviderResult response)
    {
        return TryReadError(response.RawResponse, "message") ?? response.Content ?? string.Empty;
    }

    private static string? TryReadError(string? rawResponse, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(rawResponse);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("error", out var error)
                && error.ValueKind == JsonValueKind.Object
                && error.TryGetProperty(propertyName, out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
