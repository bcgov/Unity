namespace Unity.AI.Runtime;

public sealed record AIPromptTemplateSnapshot(
    string PromptVersion,
    string SystemPrompt,
    string UserPromptTemplate,
    string? MetadataJson);
