namespace Unity.GrantManager.AI
{
    internal static class AttachmentPrompts
    {
        public static readonly string SystemPrompt = PromptHeader.Build(
            "You are a professional grant analyst for the BC Government.",
            "Produce a concise reviewer-facing summary of the provided attachment context.");

        public const string OutputSection = @"OUTPUT
- Plain text only
- 1-2 complete sentences";

        public const string RulesSection = @"RULES
- Use only the provided attachment context as evidence.
- If text content is present, summarize the actual content.
- If text content is missing or empty, provide a conservative metadata-based summary.
- Do not invent missing details.
- Keep the summary specific, concrete, and reviewer-facing.
- Return plain text only (no markdown, bullets, or JSON).";

        public static string BuildUserPrompt(string attachmentPayloadJson)
        {
            return $@"ATTACHMENT
{attachmentPayloadJson}";
        }
    }
}
