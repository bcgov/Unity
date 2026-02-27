namespace Unity.GrantManager.AI
{
    internal static class AttachmentPrompts
    {
        public static readonly string SystemPrompt = PromptHeader.Build(
            "You are a professional grant analyst for the BC Government.",
            "Produce a concise reviewer-facing summary of the provided attachment context.");

        public const string Prompt = @"Please analyze this document and provide a concise summary of its content, purpose, and key information, for use by your fellow grant analysts. It should be 1-2 sentences long and about 46 tokens.";
    }
}

