using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.AI.Evaluation;

// Stable, comparable-only baseline format. Never derived from run.json (which
// contains volatile timestamp/commit/latency/tokens). Comparison reads *only*
// these fields — mismatched datasetHash fails hard.
public sealed class Baseline
{
    [JsonPropertyName("baselineVersion")]
    public int BaselineVersion { get; set; } = 2;

    [JsonPropertyName("datasetHash")]
    public string DatasetHash { get; set; } = "";

    [JsonPropertyName("harnessVersion")]
    public string HarnessVersion { get; set; } = "";

    [JsonPropertyName("caseSetHash")]
    public string CaseSetHash { get; set; } = "";

    [JsonPropertyName("candidate")]
    public BaselineCandidate Candidate { get; set; } = new();

    [JsonPropertyName("judge")]
    public BaselineJudge Judge { get; set; } = new();

    [JsonPropertyName("aggregate")]
    public BaselineAggregate Aggregate { get; set; } = new();

    [JsonPropertyName("cases")]
    public Dictionary<string, BaselineCase> Cases { get; set; } = new();
}

public sealed class BaselineCandidate
{
    [JsonPropertyName("provider")] public string Provider { get; set; } = "";
    [JsonPropertyName("profile")] public string Profile { get; set; } = "";
    [JsonPropertyName("promptVersion")] public string PromptVersion { get; set; } = "";
    [JsonPropertyName("models")] public List<string> Models { get; set; } = new();
    [JsonPropertyName("promptTemplateHashes")] public List<string> PromptTemplateHashes { get; set; } = new();
}

public sealed class BaselineJudge
{
    [JsonPropertyName("deployment")] public string Deployment { get; set; } = "";
    [JsonPropertyName("apiVersion")] public string ApiVersion { get; set; } = "";
    [JsonPropertyName("models")] public List<string> Models { get; set; } = new();
}

public sealed class BaselineAggregate
{
    [JsonPropertyName("passRate")] public double PassRate { get; set; }
    [JsonPropertyName("meanRubric")] public double MeanRubric { get; set; }
    [JsonPropertyName("factCoverageRate")] public double FactCoverageRate { get; set; }
}

public sealed class BaselineCase
{
    [JsonPropertyName("pass")] public bool Pass { get; set; }
    [JsonPropertyName("hallucination")] public bool Hallucination { get; set; }
    [JsonPropertyName("forbiddenClaim")] public bool ForbiddenClaim { get; set; }
    [JsonPropertyName("factCoverageRate")] public double FactCoverageRate { get; set; }
    [JsonPropertyName("rubric")] public BaselineRubric Rubric { get; set; } = new();
}

public sealed class BaselineRubric
{
    [JsonPropertyName("groundedness")] public int Groundedness { get; set; }
    [JsonPropertyName("requiredFactCoverage")] public int RequiredFactCoverage { get; set; }
    [JsonPropertyName("attachmentFocus")] public int AttachmentFocus { get; set; }
    [JsonPropertyName("reviewerUsefulness")] public int ReviewerUsefulness { get; set; }
}
