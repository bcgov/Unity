using Microsoft.Extensions.Configuration;
using System;
using System.ClientModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Setup;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime.Execution;

/// <summary>
/// Makes one small call to a model's Azure deployment with the configured endpoint and key
/// (a chat message for chat models, a one-word embedding for embedding models), so a missing
/// or retired deployment shows up before anything uses it.
/// </summary>
public class AIModelConnectionTester(
    OpenAITransportService transportService,
    OpenAIChatClientFactory clientFactory,
    IConfiguration configuration) : ITransientDependency
{
    public const string EndpointNotConfigured = "EndpointNotConfigured";
    public const string ApiKeyNotConfigured = "ApiKeyNotConfigured";
    public const string ApiVersionNotConfigured = "ApiVersionNotConfigured";

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

        var apiVersion = model.ApiVersion?.Trim();
        if (!AzureOpenAIApiVersions.IsValid(apiVersion))
        {
            return Failure(ApiVersionNotConfigured);
        }

        var settings = new OpenAIOperationSettings(
            model.Provider,
            model.Name,
            apiKey,
            endpoint,
            model.Name,
            apiVersion,
            "test");

        if (model.Type == AIModelType.Embedding)
        {
            return await TestEmbeddingAsync(settings, cancellationToken);
        }

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

    private async Task<AIModelTestResultDto> TestEmbeddingAsync(
        OpenAIOperationSettings settings,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await clientFactory.CreateEmbeddingClient(settings)
                .GenerateEmbeddingAsync("OK", cancellationToken: cancellationToken);
            stopwatch.Stop();
            return new AIModelTestResultDto
            {
                Success = true,
                HttpStatusCode = 200,
                LatencyMilliseconds = stopwatch.ElapsedMilliseconds
            };
        }
        catch (ClientResultException ex)
        {
            stopwatch.Stop();
            var rawResponse = ex.GetRawResponse()?.Content?.ToString();
            return new AIModelTestResultDto
            {
                Success = false,
                HttpStatusCode = ex.Status > 0 ? ex.Status : null,
                ErrorCode = ExtractAzureErrorCode(rawResponse),
                LatencyMilliseconds = stopwatch.ElapsedMilliseconds,
                Message = TryReadError(rawResponse, "message") ?? ex.Message
            };
        }
        catch (Exception ex) when (ex is HttpRequestException
            || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            stopwatch.Stop();
            return new AIModelTestResultDto
            {
                Success = false,
                LatencyMilliseconds = stopwatch.ElapsedMilliseconds,
                Message = ex.Message
            };
        }
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
