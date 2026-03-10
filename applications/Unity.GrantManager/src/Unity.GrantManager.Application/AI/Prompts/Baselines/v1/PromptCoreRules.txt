namespace Unity.GrantManager.AI
{
    internal static class PromptCoreRules
    {
        public const string UseProvidedEvidence = "- Use only provided input sections as evidence.";
        public const string NoInvention = "- Do not invent missing details.";
        public const string MinimumNarrativeWords = "- Any narrative text response must be at least 12 words.";
        public const string ExactOutputShape = "- Return values exactly as specified in OUTPUT.";
        public const string NoExtraOutputKeys = "- Do not return keys outside OUTPUT.";
        public const string ValidJsonOnly = "- Return valid JSON only.";
        public const string PlainJsonOnly = "- Return plain JSON only (no markdown).";
    }
}
