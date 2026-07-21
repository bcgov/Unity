using System;

namespace Unity.AI.Runtime;

public sealed record AIPromptTemplateSnapshot(
    string PromptVersion,
    string SystemPrompt,
    string UserPrompt,
    string? MetadataJson)
{
}
