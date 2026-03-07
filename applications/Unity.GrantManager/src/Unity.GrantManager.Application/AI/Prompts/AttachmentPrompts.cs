namespace Unity.GrantManager.AI
{
    internal static class AttachmentPrompts
    {
        public static readonly string SystemPrompt = PromptHeader.Build(
            "You are a professional grant analyst for the BC Government.",
            "Produce a concise reviewer-facing summary of the provided attachment context.");

        public static readonly string SystemPromptV0 = PromptHeader.Build(
            "You are a professional grant analyst for the BC Government.",
            "Produce a concise reviewer-facing summary of the provided attachment context.");

        public const string OutputSection = @"OUTPUT
{
  ""summary"": ""<string>""
}";

        public const string RulesSection = "- Use only ATTACHMENT as evidence.\n"
            + "- If ATTACHMENT.text is present, summarize actual content.\n"
            + "- If ATTACHMENT.text is null or empty, provide a conservative file-level summary.\n"
            + PromptCoreRules.NoInvention + "\n"
            + @"- Write 1-2 complete sentences.
- Summary must be grounded in concrete ATTACHMENT evidence.
- Return exactly one object with only the key: summary.
"
            + PromptCoreRules.MinimumNarrativeWords + "\n"
            + PromptCoreRules.ExactOutputShape + "\n"
            + PromptCoreRules.NoExtraOutputKeys + "\n"
            + PromptCoreRules.ValidJsonOnly + "\n"
            + PromptCoreRules.PlainJsonOnly;

        public const string OutputSectionV0 = @"OUTPUT
- Plain text only
- 1-2 complete sentences";

        public const string RulesSectionV0 = @"RULES
- Use only the provided attachment context as evidence.
- If text content is present, summarize the actual content.
- If text content is missing or empty, provide a conservative metadata-based summary.
- Do not invent missing details.
- Keep the summary specific, concrete, and reviewer-facing.
- Return plain text only (no markdown, bullets, or JSON).";

        public static string GetSystemPrompt(bool useV0) => useV0 ? SystemPromptV0 : SystemPrompt;
        public static string GetOutputSection(bool useV0) => useV0 ? OutputSectionV0 : OutputSection;
        public static string GetRulesSection(bool useV0) => useV0 ? RulesSectionV0 : RulesSection;

        public static string BuildUserPrompt(string attachmentPayloadJson)
        {
            return BuildUserPrompt(attachmentPayloadJson, useV0: false);
        }

        public static string BuildUserPrompt(string attachmentPayloadJson, bool useV0)
        {
            return $@"ATTACHMENT
{attachmentPayloadJson}";
        }
    }
}
