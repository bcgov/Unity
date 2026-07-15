using System.Text.Json.Serialization;

namespace Unity.AI.Prompts;

public sealed record UnityPromptAssetManifest(
    [property: JsonPropertyName("operationName")] string OperationName,
    [property: JsonPropertyName("promptVersion")] string PromptVersion,
    [property: JsonPropertyName("inputContractName")] string InputContractName,
    [property: JsonPropertyName("outputContractName")] string OutputContractName,
    [property: JsonPropertyName("modelHint")] string? ModelHint = null,
    [property: JsonPropertyName("profileHint")] string? ProfileHint = null);
