using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.AI.Evaluation;

// Env-only credentials. Kept off ABP config surface deliberately.
public sealed class AzureOpenAIJudgeClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deployment;
    private readonly string _apiVersion;

    public AzureOpenAIJudgeClient()
    {
        _endpoint = RequireEnv("EVAL_JUDGE_ENDPOINT").TrimEnd('/');
        _apiKey = RequireEnv("EVAL_JUDGE_KEY");
        _deployment = RequireEnv("EVAL_JUDGE_DEPLOYMENT");
        _apiVersion = RequireEnv("EVAL_JUDGE_API_VERSION");
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
    }

    public string DeploymentName => _deployment;
    public string ApiVersion => _apiVersion;

    public async Task<JudgeVerdict> JudgeAsync(
        EvalCase evalCase,
        string extractedText,
        string candidateSummary,
        bool extractionStoppedOnEmpty,
        CancellationToken cancellationToken)
    {
        var url = $"{_endpoint}/openai/deployments/{_deployment}/chat/completions?api-version={_apiVersion}";
        var systemPrompt = JudgePrompts.System;
        var expectedFacts = BuildExpectedFacts(evalCase);
        var hallucinationTraps = BuildHallucinationTraps(evalCase);
        var userPayload = new
        {
            fileName = evalCase.FileName,
            contentType = evalCase.ContentType,
            extractedText,
            extractionStoppedOnEmpty,
            candidateSummary,
            baselineSummary = evalCase.ReferenceSummary,
            expectedFacts,
            hallucinationTraps,
        };

        var request = new
        {
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = JsonSerializer.Serialize(userPayload) },
            },
            response_format = BuildResponseFormat(expectedFacts, hallucinationTraps),
            max_completion_tokens = 12000,
        };

        var stopwatch = Stopwatch.StartNew();
        var usage = JudgeUsage.Empty;
        var lastModel = "";
        var lastValidationFailure = "no valid response";

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var msg = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(request),
            };
            msg.Headers.Add("api-key", _apiKey);

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(msg, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                if (attempt == 3)
                {
                    return JudgeVerdict.Failure($"transport error: {ex.GetType().Name}")
                        with { JudgeDurationMs = stopwatch.ElapsedMilliseconds, JudgeAttemptCount = attempt };
                }
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
                continue;
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    if (attempt == 3)
                    {
                        return JudgeVerdict.Failure($"http {(int)response.StatusCode}")
                            with { JudgeDurationMs = stopwatch.ElapsedMilliseconds, JudgeAttemptCount = attempt };
                    }
                    await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
                    continue;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (TryExtractVerdict(
                        body,
                        expectedFacts.Select(fact => fact.Id).ToList(),
                        hallucinationTraps.Select(trap => trap.Id).ToList(),
                        out var verdict,
                        out var responseMetadata))
                {
                    usage += responseMetadata.Usage;
                    return verdict with
                    {
                        JudgeDurationMs = stopwatch.ElapsedMilliseconds,
                        JudgeAttemptCount = attempt,
                        JudgePromptTokens = usage.Prompt,
                        JudgeCompletionTokens = usage.Completion,
                        JudgeTotalTokens = usage.Total,
                        JudgeReasoningTokens = usage.Reasoning,
                    };
                }

                usage += responseMetadata.Usage;
                lastModel = responseMetadata.Model;
                lastValidationFailure = responseMetadata.ValidationFailure;
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }

        return JudgeVerdict.Failure(
                $"malformed judge response after 3 attempts: {lastValidationFailure}")
            with
            {
                JudgeDurationMs = stopwatch.ElapsedMilliseconds,
                JudgeAttemptCount = 3,
                JudgeModel = lastModel,
                JudgePromptTokens = usage.Prompt,
                JudgeCompletionTokens = usage.Completion,
                JudgeTotalTokens = usage.Total,
                JudgeReasoningTokens = usage.Reasoning,
            };
    }

    internal static bool TryExtractVerdict(
        string body,
        IReadOnlyList<string> expectedFactIds,
        IReadOnlyList<string> expectedTrapIds,
        out JudgeVerdict verdict) =>
        TryExtractVerdict(
            body,
            expectedFactIds,
            expectedTrapIds,
            out verdict,
            out _);

    internal static bool TryExtractVerdict(
        string body,
        IReadOnlyList<string> expectedFactIds,
        IReadOnlyList<string> expectedTrapIds,
        out JudgeVerdict verdict,
        out JudgeResponseMetadata metadata)
    {
        verdict = JudgeVerdict.Failure("unparsed");
        metadata = JudgeResponseMetadata.Empty;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var judgeModel = doc.RootElement.TryGetProperty("model", out var modelProperty)
                && modelProperty.ValueKind == JsonValueKind.String
                    ? modelProperty.GetString() ?? ""
                    : "";
            var (judgePromptTokens, judgeCompletionTokens, judgeTotalTokens, judgeReasoningTokens) =
                ExtractUsage(doc.RootElement);
            var usage = new JudgeUsage(
                judgePromptTokens,
                judgeCompletionTokens,
                judgeTotalTokens,
                judgeReasoningTokens);
            metadata = new JudgeResponseMetadata(judgeModel, usage, "unparsed response");
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            if (string.IsNullOrWhiteSpace(content))
            {
                metadata = metadata with { ValidationFailure = "empty content" };
                return false;
            }

            JudgeVerdictDto? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<JudgeVerdictDto>(content);
            }
            catch (JsonException)
            {
                metadata = metadata with { ValidationFailure = "invalid content JSON" };
                return false;
            }
            if (parsed == null)
            {
                metadata = metadata with { ValidationFailure = "empty content object" };
                return false;
            }

            if (!ValidScore(parsed.Groundedness)
                || !ValidScore(parsed.AttachmentFocus)
                || !ValidScore(parsed.ReviewerUsefulness))
            {
                metadata = metadata with { ValidationFailure = "invalid rubric score" };
                return false;
            }
            if (string.IsNullOrWhiteSpace(parsed.Rationale))
            {
                metadata = metadata with { ValidationFailure = "missing rationale" };
                return false;
            }
            if (!TryValidateFactAssessments(
                    parsed.FactAssessments,
                    expectedFactIds,
                    out var coveredFactCount,
                    out var partialFactIds,
                    out var missingFactIds,
                    out var factCoverageRate))
            {
                metadata = metadata with { ValidationFailure = "invalid fact assessments" };
                return false;
            }
            if (!TryValidateTrapAssessments(
                    parsed.TrapAssessments,
                    expectedTrapIds,
                    out var triggeredTrapIds))
            {
                metadata = metadata with { ValidationFailure = "invalid trap assessments" };
                return false;
            }
            if (!TryValidateClaimAssessments(
                    parsed.ClaimAssessments,
                    out var unsupportedClaimCount,
                    out var hallucinationSeverity))
            {
                metadata = metadata with { ValidationFailure = "invalid claim assessments" };
                return false;
            }

            verdict = new JudgeVerdict(
                Groundedness: parsed.Groundedness,
                RequiredFactCoverage: CoverageScore(factCoverageRate),
                AttachmentFocus: parsed.AttachmentFocus,
                ReviewerUsefulness: parsed.ReviewerUsefulness,
                Hallucination: unsupportedClaimCount > 0,
                ForbiddenClaim: triggeredTrapIds.Count > 0,
                FailureReason: null)
            {
                FactCoverageRate = factCoverageRate,
                CoveredFactCount = coveredFactCount,
                PartialFactCount = partialFactIds.Count,
                MissingFactCount = missingFactIds.Count,
                PartialFactIds = partialFactIds,
                MissingFactIds = missingFactIds,
                TriggeredTrapIds = triggeredTrapIds,
                UnsupportedClaimCount = unsupportedClaimCount,
                HallucinationSeverity = hallucinationSeverity,
                JudgeModel = judgeModel,
                JudgePromptTokens = judgePromptTokens,
                JudgeCompletionTokens = judgeCompletionTokens,
                JudgeTotalTokens = judgeTotalTokens,
                JudgeReasoningTokens = judgeReasoningTokens,
                Audit = new JudgeAuditDetails(
                    parsed.Rationale,
                    parsed.FactAssessments,
                    parsed.ClaimAssessments,
                    parsed.TrapAssessments),
            };
            metadata = metadata with { ValidationFailure = "" };
            return true;
        }
        catch (JsonException)
        {
            metadata = metadata with { ValidationFailure = "invalid response JSON" };
            return false;
        }
        catch (InvalidOperationException)
        {
            metadata = metadata with { ValidationFailure = "invalid response envelope" };
            return false;
        }
        catch (KeyNotFoundException)
        {
            metadata = metadata with { ValidationFailure = "invalid response envelope" };
            return false;
        }
    }

    private static object BuildResponseFormat(
        IReadOnlyList<JudgeFactSpec> expectedFacts,
        IReadOnlyList<JudgeTrapSpec> hallucinationTraps) =>
        new
        {
            type = "json_schema",
            json_schema = new
            {
                name = "attachment_summary_judge",
                strict = true,
                schema = new
                {
                    type = "object",
                    properties = new
                    {
                        groundedness = IntegerScoreSchema(),
                        attachmentFocus = IntegerScoreSchema(),
                        reviewerUsefulness = IntegerScoreSchema(),
                        rationale = NonEmptyStringSchema(),
                        factAssessments = new
                        {
                            type = "array",
                            minItems = expectedFacts.Count,
                            maxItems = expectedFacts.Count,
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    factId = new
                                    {
                                        type = "string",
                                        @enum = expectedFacts.Select(fact => fact.Id)
                                            .DefaultIfEmpty("__none__")
                                            .ToArray(),
                                    },
                                    coverage = new
                                    {
                                        type = "string",
                                        @enum = new[] { "covered", "partial", "missing" },
                                    },
                                    rationale = NonEmptyStringSchema(),
                                },
                                required = new[] { "factId", "coverage", "rationale" },
                                additionalProperties = false,
                            },
                        },
                        claimAssessments = new
                        {
                            type = "array",
                            minItems = 1,
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    claim = NonEmptyStringSchema(),
                                    support = new
                                    {
                                        type = "string",
                                        @enum = new[] { "supported", "ambiguous", "unsupported" },
                                    },
                                    evidence = NonEmptyStringSchema(),
                                    severity = new
                                    {
                                        type = "string",
                                        @enum = new[] { "none", "minor", "material", "critical" },
                                    },
                                },
                                required = new[] { "claim", "support", "evidence", "severity" },
                                additionalProperties = false,
                            },
                        },
                        trapAssessments = new
                        {
                            type = "array",
                            minItems = hallucinationTraps.Count,
                            maxItems = hallucinationTraps.Count,
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    trapId = new
                                    {
                                        type = "string",
                                        @enum = hallucinationTraps.Select(trap => trap.Id)
                                            .DefaultIfEmpty("__none__")
                                            .ToArray(),
                                    },
                                    triggered = new { type = "boolean" },
                                    rationale = NonEmptyStringSchema(),
                                },
                                required = new[] { "trapId", "triggered", "rationale" },
                                additionalProperties = false,
                            },
                        },
                    },
                    required = new[]
                    {
                        "groundedness",
                        "attachmentFocus",
                        "reviewerUsefulness",
                        "rationale",
                        "factAssessments",
                        "claimAssessments",
                        "trapAssessments",
                    },
                    additionalProperties = false,
                },
            },
        };

    private static object IntegerScoreSchema() => new
    {
        type = "integer",
        minimum = 1,
        maximum = 5,
    };

    private static object NonEmptyStringSchema() => new
    {
        type = "string",
        minLength = 1,
    };

    // Azure OpenAI chat completions responses report token usage even when the
    // model call succeeds but our own validation later rejects the payload, so
    // this reads directly off the raw JSON rather than the parsed judge DTO.
    private static (int Prompt, int Completion, int Total, int Reasoning) ExtractUsage(JsonElement root)
    {
        if (!root.TryGetProperty("usage", out var usage) || usage.ValueKind != JsonValueKind.Object)
        {
            return (0, 0, 0, 0);
        }

        var prompt = GetInt(usage, "prompt_tokens");
        var completion = GetInt(usage, "completion_tokens");
        var total = GetInt(usage, "total_tokens");
        var reasoning = usage.TryGetProperty("completion_tokens_details", out var details)
            && details.ValueKind == JsonValueKind.Object
                ? GetInt(details, "reasoning_tokens")
                : 0;
        return (prompt, completion, total, reasoning);
    }

    private static int GetInt(JsonElement obj, string propertyName) =>
        obj.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : 0;

    private static bool ValidScore(int score) => score is >= 1 and <= 5;

    private static List<JudgeFactSpec> BuildExpectedFacts(EvalCase evalCase)
    {
        if (evalCase.FactEvidence.Count == evalCase.ExpectedFacts.Count
            && evalCase.FactEvidence.Count > 0)
        {
            return evalCase.FactEvidence
                .Select(fact => new JudgeFactSpec(fact.Id, fact.Text, fact.Evidence))
                .ToList();
        }

        return evalCase.ExpectedFacts
            .Select((text, index) => new JudgeFactSpec($"f{index + 1}", text, "fixture text"))
            .ToList();
    }

    private static List<JudgeTrapSpec> BuildHallucinationTraps(EvalCase evalCase)
    {
        if (evalCase.HallucinationTraps.Count > 0)
        {
            return evalCase.HallucinationTraps
                .Select(trap => new JudgeTrapSpec(trap.Id, trap.Type, trap.ForbiddenClaim))
                .ToList();
        }

        return evalCase.ForbiddenClaims
            .Select((claim, index) => new JudgeTrapSpec(
                $"t{index + 1}",
                "literal_forbidden_claim",
                claim))
            .ToList();
    }

    private static bool TryValidateFactAssessments(
        IReadOnlyList<JudgeFactAssessment> assessments,
        IReadOnlyList<string> expectedIds,
        out int coveredCount,
        out IReadOnlyList<string> partialIds,
        out IReadOnlyList<string> missingIds,
        out double coverageRate)
    {
        coveredCount = 0;
        partialIds = Array.Empty<string>();
        missingIds = Array.Empty<string>();
        coverageRate = 0;

        if (!HasExactlyExpectedIds(assessments.Select(item => item.FactId), expectedIds))
        {
            return false;
        }

        var partial = new List<string>();
        var missing = new List<string>();
        foreach (var assessment in assessments)
        {
            if (string.IsNullOrWhiteSpace(assessment.Rationale))
            {
                return false;
            }

            switch (assessment.Coverage?.Trim().ToLowerInvariant())
            {
                case "covered":
                    coveredCount++;
                    break;
                case "partial":
                    partial.Add(assessment.FactId);
                    break;
                case "missing":
                    missing.Add(assessment.FactId);
                    break;
                default:
                    return false;
            }
        }

        partialIds = partial;
        missingIds = missing;
        coverageRate = expectedIds.Count == 0
            ? 1
            : (coveredCount + (partial.Count * 0.5)) / expectedIds.Count;
        return true;
    }

    private static bool TryValidateTrapAssessments(
        IReadOnlyList<JudgeTrapAssessment> assessments,
        IReadOnlyList<string> expectedIds,
        out IReadOnlyList<string> triggeredIds)
    {
        triggeredIds = Array.Empty<string>();
        if (!HasExactlyExpectedIds(assessments.Select(item => item.TrapId), expectedIds)
            || assessments.Any(item => string.IsNullOrWhiteSpace(item.Rationale)))
        {
            return false;
        }

        triggeredIds = assessments
            .Where(item => item.Triggered)
            .Select(item => item.TrapId)
            .ToList();
        return true;
    }

    private static bool TryValidateClaimAssessments(
        IReadOnlyList<JudgeClaimAssessment> assessments,
        out int unsupportedCount,
        out string severity)
    {
        unsupportedCount = 0;
        severity = "none";
        if (assessments.Count == 0)
        {
            return false;
        }

        var severityRank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["none"] = 0,
            ["minor"] = 1,
            ["material"] = 2,
            ["critical"] = 3,
        };
        var highestRank = 0;
        foreach (var assessment in assessments)
        {
            var support = assessment.Support?.Trim().ToLowerInvariant();
            var normalizedSeverity = assessment.Severity?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(assessment.Claim)
                || string.IsNullOrWhiteSpace(assessment.Evidence)
                || support is not ("supported" or "ambiguous" or "unsupported")
                || normalizedSeverity == null
                || !severityRank.TryGetValue(normalizedSeverity, out var rank))
            {
                return false;
            }

            if (support == "unsupported")
            {
                if (rank == 0)
                {
                    return false;
                }
                unsupportedCount++;
                if (rank > highestRank)
                {
                    highestRank = rank;
                    severity = normalizedSeverity;
                }
            }
            // Severity is only actionable for unsupported claims. Some judge
            // responses attach a hypothetical severity to an ambiguous claim;
            // ignore it instead of rejecting an otherwise complete verdict.
        }

        return true;
    }

    private static bool HasExactlyExpectedIds(
        IEnumerable<string> actualIds,
        IReadOnlyList<string> expectedIds)
    {
        var actual = actualIds.ToList();
        return actual.Count == expectedIds.Count
            && actual.Distinct(StringComparer.Ordinal).Count() == actual.Count
            && actual.OrderBy(id => id, StringComparer.Ordinal)
                .SequenceEqual(expectedIds.OrderBy(id => id, StringComparer.Ordinal));
    }

    private static int CoverageScore(double coverageRate) => coverageRate switch
    {
        >= 1.0 => 5,
        >= 0.8 => 4,
        >= 0.5 => 3,
        > 0 => 2,
        _ => 1,
    };

    private static string RequireEnv(string name)
    {
        var v = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(v))
        {
            throw new InvalidOperationException($"Missing required env var: {name}");
        }
        return v;
    }

    public void Dispose() => _http.Dispose();

    private sealed class JudgeVerdictDto
    {
        [JsonPropertyName("groundedness")] public int Groundedness { get; set; }
        [JsonPropertyName("attachmentFocus")] public int AttachmentFocus { get; set; }
        [JsonPropertyName("reviewerUsefulness")] public int ReviewerUsefulness { get; set; }
        [JsonPropertyName("rationale")] public string Rationale { get; set; } = "";
        [JsonPropertyName("factAssessments")] public List<JudgeFactAssessment> FactAssessments { get; set; } = new();
        [JsonPropertyName("claimAssessments")] public List<JudgeClaimAssessment> ClaimAssessments { get; set; } = new();
        [JsonPropertyName("trapAssessments")] public List<JudgeTrapAssessment> TrapAssessments { get; set; } = new();
    }
}

public sealed record JudgeFactSpec(string Id, string Text, string Evidence);
public sealed record JudgeTrapSpec(string Id, string Type, string ForbiddenClaim);

public sealed class JudgeFactAssessment
{
    [JsonPropertyName("factId")] public string FactId { get; set; } = "";
    [JsonPropertyName("coverage")] public string Coverage { get; set; } = "";
    [JsonPropertyName("rationale")] public string Rationale { get; set; } = "";
}

public sealed class JudgeClaimAssessment
{
    [JsonPropertyName("claim")] public string Claim { get; set; } = "";
    [JsonPropertyName("support")] public string Support { get; set; } = "";
    [JsonPropertyName("evidence")] public string Evidence { get; set; } = "";
    [JsonPropertyName("severity")] public string Severity { get; set; } = "none";
}

public sealed class JudgeTrapAssessment
{
    [JsonPropertyName("trapId")] public string TrapId { get; set; } = "";
    [JsonPropertyName("triggered")] public bool Triggered { get; set; }
    [JsonPropertyName("rationale")] public string Rationale { get; set; } = "";
}

public sealed record JudgeAuditDetails(
    string Rationale,
    IReadOnlyList<JudgeFactAssessment> FactAssessments,
    IReadOnlyList<JudgeClaimAssessment> ClaimAssessments,
    IReadOnlyList<JudgeTrapAssessment> TrapAssessments);

internal readonly record struct JudgeUsage(
    int Prompt,
    int Completion,
    int Total,
    int Reasoning)
{
    public static JudgeUsage Empty => new(0, 0, 0, 0);

    public static JudgeUsage operator +(JudgeUsage left, JudgeUsage right) =>
        new(
            left.Prompt + right.Prompt,
            left.Completion + right.Completion,
            left.Total + right.Total,
            left.Reasoning + right.Reasoning);
}

internal sealed record JudgeResponseMetadata(
    string Model,
    JudgeUsage Usage,
    string ValidationFailure)
{
    public static JudgeResponseMetadata Empty => new("", JudgeUsage.Empty, "unparsed response");
}

public sealed record JudgeVerdict(
    int Groundedness,
    int RequiredFactCoverage,
    int AttachmentFocus,
    int ReviewerUsefulness,
    bool Hallucination,
    bool ForbiddenClaim,
    string? FailureReason)
{
    public double FactCoverageRate { get; init; }
    public int CoveredFactCount { get; init; }
    public int PartialFactCount { get; init; }
    public int MissingFactCount { get; init; }
    public IReadOnlyList<string> PartialFactIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MissingFactIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TriggeredTrapIds { get; init; } = Array.Empty<string>();
    public int UnsupportedClaimCount { get; init; }
    public string HallucinationSeverity { get; init; } = "none";
    public string JudgeModel { get; init; } = "";
    public long JudgeDurationMs { get; init; }
    public int JudgeAttemptCount { get; init; }
    public int JudgePromptTokens { get; init; }
    public int JudgeCompletionTokens { get; init; }
    public int JudgeTotalTokens { get; init; }
    public int JudgeReasoningTokens { get; init; }
    public string? SkippedReason { get; init; }
    [JsonIgnore] public JudgeAuditDetails? Audit { get; init; }

    public bool Failed => FailureReason != null;
    public bool Skipped => SkippedReason != null;
    public bool Evaluated => !Failed && !Skipped;
    public double MeanRubric => Evaluated ? (Groundedness + RequiredFactCoverage + AttachmentFocus + ReviewerUsefulness) / 4.0 : 0;
    public bool AllDimsAtLeast3 => Evaluated && Groundedness >= 3 && RequiredFactCoverage >= 3 && AttachmentFocus >= 3 && ReviewerUsefulness >= 3;
    public bool HasMinorUnsupportedClaimWarning =>
        Hallucination
        && string.Equals(HallucinationSeverity, "minor", StringComparison.OrdinalIgnoreCase);
    public bool HasBlockingUnsupportedClaim =>
        Hallucination && !HasMinorUnsupportedClaimWarning;

    public static JudgeVerdict Failure(string reason) =>
        new(0, 0, 0, 0, false, false, reason);

    public static JudgeVerdict Skip(string reason) =>
        new(0, 0, 0, 0, false, false, null) { SkippedReason = reason };
}

internal static class JudgePrompts
{
    // Prompt is intentionally short and rubric-focused. Only case + summary go to the judge — never
    // the candidate's rendered prompt, provider RawResponse, keys, or endpoints.
    public const string System = """
        You are an evaluator scoring a candidate summary of an attachment submitted with a grant
        application. Score each dimension on 1-5. Return ONLY valid JSON matching this schema:
        {
          "groundedness": 1..5,
          "attachmentFocus": 1..5,
          "reviewerUsefulness": 1..5,
          "rationale": "brief overall explanation",
          "factAssessments": [
            { "factId": "exact expectedFacts id", "coverage": "covered|partial|missing", "rationale": "why" }
          ],
          "claimAssessments": [
            { "claim": "one candidate claim", "support": "supported|ambiguous|unsupported", "evidence": "support or gap", "severity": "none|minor|material|critical" }
          ],
          "trapAssessments": [
            { "trapId": "exact hallucinationTraps id", "triggered": true|false, "rationale": "why" }
          ]
        }
        """;
}
