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
    public UnityPromptAssetManifest? Manifest => ParseManifest(MetadataJson);

    private static UnityPromptAssetManifest? ParseManifest(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

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
