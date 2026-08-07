using System;
using System.Collections.Generic;
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
        _endpoint  = RequireEnv("EVAL_JUDGE_ENDPOINT").TrimEnd('/');
        _apiKey    = RequireEnv("EVAL_JUDGE_KEY");
        _deployment = RequireEnv("EVAL_JUDGE_DEPLOYMENT");
        _apiVersion = RequireEnv("EVAL_JUDGE_API_VERSION");
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
    }

    public string DeploymentName => _deployment;

    public async Task<JudgeVerdict> JudgeAsync(
        EvalCase evalCase,
        string extractedText,
        string candidateSummary,
        bool extractionStoppedOnEmpty,
        CancellationToken cancellationToken)
    {
        var url = $"{_endpoint}/openai/deployments/{_deployment}/chat/completions?api-version={_apiVersion}";
        var systemPrompt = JudgePrompts.System;
        var userPayload = new
        {
            fileName = evalCase.FileName,
            contentType = evalCase.ContentType,
            extractedText,
            extractionStoppedOnEmpty,
            candidateSummary,
            baselineSummary = evalCase.ReferenceSummary,
            expectedFacts = evalCase.ExpectedFacts,
            factEvidence = evalCase.FactEvidence,
            forbiddenClaims = evalCase.ForbiddenClaims,
            hallucinationTraps = evalCase.HallucinationTraps,
        };

        var request = new
        {
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = JsonSerializer.Serialize(userPayload) },
            },
            response_format = new { type = "json_object" },
        };

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
                    return JudgeVerdict.Failure($"transport error: {ex.GetType().Name}");
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
                        return JudgeVerdict.Failure($"http {(int)response.StatusCode}");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
                    continue;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (TryExtractVerdict(body, out var verdict))
                {
                    return verdict;
                }
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }

        return JudgeVerdict.Failure("malformed judge response after 3 attempts");
    }

    private static bool TryExtractVerdict(string body, out JudgeVerdict verdict)
    {
        verdict = JudgeVerdict.Failure("unparsed");
        try
        {
            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            var parsed = JsonSerializer.Deserialize<JudgeVerdictDto>(content);
            if (parsed == null)
            {
                return false;
            }

            if (!ValidScore(parsed.Groundedness)
                || !ValidScore(parsed.RequiredFactCoverage)
                || !ValidScore(parsed.AttachmentFocus)
                || !ValidScore(parsed.ReviewerUsefulness))
            {
                return false;
            }

            verdict = new JudgeVerdict(
                Groundedness: parsed.Groundedness,
                RequiredFactCoverage: parsed.RequiredFactCoverage,
                AttachmentFocus: parsed.AttachmentFocus,
                ReviewerUsefulness: parsed.ReviewerUsefulness,
                Hallucination: parsed.Hallucination,
                ForbiddenClaim: parsed.ForbiddenClaim,
                FailureReason: null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidScore(int score) => score is >= 1 and <= 5;

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
        [JsonPropertyName("groundedness")]         public int Groundedness { get; set; }
        [JsonPropertyName("requiredFactCoverage")] public int RequiredFactCoverage { get; set; }
        [JsonPropertyName("attachmentFocus")]      public int AttachmentFocus { get; set; }
        [JsonPropertyName("reviewerUsefulness")]   public int ReviewerUsefulness { get; set; }
        [JsonPropertyName("hallucination")]        public bool Hallucination { get; set; }
        [JsonPropertyName("forbiddenClaim")]       public bool ForbiddenClaim { get; set; }
    }
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
    public bool Failed => FailureReason != null;
    public double MeanRubric => Failed ? 0 : (Groundedness + RequiredFactCoverage + AttachmentFocus + ReviewerUsefulness) / 4.0;
    public bool AllDimsAtLeast3 => !Failed && Groundedness >= 3 && RequiredFactCoverage >= 3 && AttachmentFocus >= 3 && ReviewerUsefulness >= 3;

    public static JudgeVerdict Failure(string reason) =>
        new(0, 0, 0, 0, false, false, reason);
}

internal static class JudgePrompts
{
    // Prompt is intentionally short and rubric-focused. Only case + summary go to the judge — never
    // the candidate's rendered prompt, provider RawResponse, keys, or endpoints.
    public const string System = """
        You are an evaluator scoring a candidate summary of an attachment submitted with a grant
        application. Score each dimension on 1-5. Return ONLY valid JSON matching this schema:
        {
          "groundedness": 1..5,          // is every claim supported by extractedText or extraction state
          "requiredFactCoverage": 1..5,  // fraction of expectedFacts present
          "attachmentFocus": 1..5,       // does it summarize the attachment (not the applicant/app broadly)
          "reviewerUsefulness": 1..5,    // would a grant reviewer find this useful
          "hallucination": true|false,   // any claim not in extractedText
          "forbiddenClaim": true|false   // any prohibited claim/category appears, interpreted semantically
        }
        Be conservative on hallucination: a single unsupported claim => true.
        factEvidence contains reviewer-verified atomic facts and their source locations.
        hallucinationTraps contains typed semantic constraints; apply them even when wording differs.
        When extractionStoppedOnEmpty is true, a summary that only reports that text could not be
        extracted is grounded and is not a hallucination. Treat forbiddenClaims as semantic
        constraints; they are not necessarily literal strings.
        """;
}
