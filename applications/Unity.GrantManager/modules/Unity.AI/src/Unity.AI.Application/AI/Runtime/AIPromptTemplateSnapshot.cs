using System;
using System.Text.Json;
using Unity.AI.Prompts;

namespace Unity.AI.Runtime;

public sealed record AIPromptTemplateSnapshot(
    string PromptVersion,
    string SystemPrompt,
    string UserPrompt,
    string? MetadataJson)
{
    public UnityPromptAssetManifest? Manifest { get; } = ParseManifest(MetadataJson);

    private static UnityPromptAssetManifest? ParseManifest(string? metadataJson)
        => string.IsNullOrWhiteSpace(metadataJson)
            ? null
            : TryDeserialize(metadataJson);

    private static UnityPromptAssetManifest? TryDeserialize(string metadataJson)
    {
        try
        {
            return JsonSerializer.Deserialize<UnityPromptAssetManifest>(metadataJson);
        }
        catch
        {
            return null;
        }
    }
}
