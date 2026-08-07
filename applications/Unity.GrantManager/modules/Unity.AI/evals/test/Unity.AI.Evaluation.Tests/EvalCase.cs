using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.AI.Evaluation;

public sealed class EvalCase
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = "";

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = "";

    [JsonPropertyName("fixturePath")]
    public string? FixturePath { get; set; }

    // ponytail: pre-extracted text on disk; bypasses TextExtractionService entirely
    [JsonPropertyName("extractedTextPath")]
    public string? ExtractedTextPath { get; set; }

    [JsonPropertyName("extractedText")]
    public string? ExtractedText { get; set; }

    [JsonPropertyName("promptVersion")]
    public string? PromptVersion { get; set; }

    [JsonPropertyName("expectedFacts")]
    public List<string> ExpectedFacts { get; set; } = new();

    [JsonPropertyName("forbiddenClaims")]
    public List<string> ForbiddenClaims { get; set; } = new();

    [JsonPropertyName("referenceSummary")]
    public string? ReferenceSummary { get; set; }

    [JsonPropertyName("reviewerNotes")]
    public string? ReviewerNotes { get; set; }

    [JsonPropertyName("documentType")]
    public string? DocumentType { get; set; }

    [JsonPropertyName("documentState")]
    public string? DocumentState { get; set; }

    [JsonPropertyName("difficulty")]
    public string? Difficulty { get; set; }

    [JsonPropertyName("trapTypes")]
    public List<string> TrapTypes { get; set; } = new();

    [JsonPropertyName("extractionStatus")]
    public string? ExtractionStatus { get; set; }

    [JsonPropertyName("expectedExtractedTextLength")]
    public int? ExpectedExtractedTextLength { get; set; }

    [JsonPropertyName("expectedExtractedTextSha256")]
    public string? ExpectedExtractedTextSha256 { get; set; }

    [JsonPropertyName("factEvidence")]
    public List<EvalFact> FactEvidence { get; set; } = new();

    [JsonPropertyName("hallucinationTraps")]
    public List<EvalHallucinationTrap> HallucinationTraps { get; set; } = new();

    // ponytail: CSV-loaded cases point at real downloaded attachments outside the
    // datasets/ tree. Not serialized; DatasetLoader sets it after resolving the file.
    [JsonIgnore]
    public string? AttachmentAbsolutePath { get; set; }

    [JsonIgnore]
    public string Source { get; set; } = "jsonl";
}

public sealed class EvalFact
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("evidence")]
    public string Evidence { get; set; } = "";
}

public sealed class EvalHallucinationTrap
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("forbiddenClaim")]
    public string ForbiddenClaim { get; set; } = "";
}
