using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime;

public class OpenAIPromptFileLogger(
    IConfiguration configuration,
    ILogger<OpenAIPromptFileLogger> logger) : ITransientDependency
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<OpenAIPromptFileLogger> _logger = logger;
    private const string PromptLogDirectoryName = "logs";
    private static readonly string PromptLogFileName = $"ai-prompts-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log";

    public Task LogPromptInputAsync(string promptType, string promptVersion, string? systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        return WritePromptLogFileAsync(promptType, promptVersion, "INPUT", FormatPromptInputForLog(systemPrompt, userPrompt), cancellationToken);
    }

    public Task LogPromptOutputAsync(string promptType, string promptVersion, string output, CancellationToken cancellationToken = default)
    {
        return WritePromptLogFileAsync(promptType, promptVersion, "OUTPUT", FormatPromptOutputForLog(output), cancellationToken);
    }

    private bool CanWritePromptFileLog()
    {
        return _configuration.GetValue<bool?>("Azure:Logging:EnablePromptFileLog") ?? false;
    }

    private async Task WritePromptLogFileAsync(string promptType, string promptVersion, string payloadType, string payload, CancellationToken cancellationToken = default)
    {
        if (!CanWritePromptFileLog())
        {
            return;
        }

        try
        {
            var now = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz");
            var logDirectory = Path.Combine(AppContext.BaseDirectory, PromptLogDirectoryName);
            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, PromptLogFileName);
            var entry = $"{now} [{promptType}] [{promptVersion}] {payloadType}\n{payload}\n\n";
            await File.AppendAllTextAsync(logPath, entry, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write AI prompt log file.");
        }
    }

    private static string FormatPromptInputForLog(string? systemPrompt, string userPrompt)
    {
        var normalizedSystemPrompt = string.IsNullOrWhiteSpace(systemPrompt) ? string.Empty : systemPrompt.Trim();
        var normalizedUserPrompt = string.IsNullOrWhiteSpace(userPrompt) ? string.Empty : userPrompt.Trim();
        return $"SYSTEM_PROMPT\n{normalizedSystemPrompt}\n\nUSER_PROMPT\n{normalizedUserPrompt}";
    }

    private static string FormatPromptOutputForLog(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return string.Empty;
        }

        if (TryFormatProviderOutput(output, out var formattedProviderOutput))
        {
            return formattedProviderOutput;
        }

        if (TryParseJsonObjectFromResponse(output, out var jsonObject))
        {
            return JsonSerializer.Serialize(jsonObject, AIJsonDefaults.Indented);
        }

        return output.Trim();
    }

    private static bool TryFormatProviderOutput(string output, out string formattedOutput)
    {
        formattedOutput = string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(output);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("choices", out var choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() == 0)
            {
                return false;
            }

            var firstChoice = choices[0];
            var content = TryGetChoiceContent(firstChoice);
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            var lines = new List<string>();

            if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
            {
                var promptTokens = TryGetInt32(usage, "prompt_tokens");
                var completionTokens = TryGetInt32(usage, "completion_tokens");
                int? reasoningTokens = null;

                if (usage.TryGetProperty("completion_tokens_details", out var completionTokenDetails)
                    && completionTokenDetails.ValueKind == JsonValueKind.Object)
                {
                    reasoningTokens = TryGetInt32(completionTokenDetails, "reasoning_tokens");
                }

                if (promptTokens.HasValue)
                {
                    lines.Add($"PromptTokens: {promptTokens.Value}");
                }

                if (completionTokens.HasValue)
                {
                    lines.Add($"CompletionTokens: {completionTokens.Value}");
                }

                if (reasoningTokens.HasValue)
                {
                    lines.Add($"ReasoningTokens: {reasoningTokens.Value}");
                }
            }

            var normalizedContent = FormatPromptOutputContent(content);
            lines.Add("Output:");
            lines.Add(normalizedContent);
            formattedOutput = string.Join(Environment.NewLine, lines);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? TryGetChoiceContent(JsonElement firstChoice)
    {
        if (!firstChoice.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!message.TryGetProperty("content", out var contentProp) || contentProp.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return contentProp.GetString();
    }

    private static string FormatPromptOutputContent(string content)
    {
        if (TryParseJsonObjectFromResponse(content, out var contentObject))
        {
            return JsonSerializer.Serialize(contentObject, AIJsonDefaults.Indented);
        }

        return content.Trim();
    }

    private static bool TryParseJsonObjectFromResponse(string response, out JsonElement objectElement)
    {
        objectElement = default;
        var cleaned = AIResponseJson.CleanJsonResponse(response);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(cleaned);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            objectElement = doc.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static int? TryGetInt32(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop)
            && prop.ValueKind == JsonValueKind.Number
            && prop.TryGetInt32(out var value)
            ? value
            : null;
    }
}
