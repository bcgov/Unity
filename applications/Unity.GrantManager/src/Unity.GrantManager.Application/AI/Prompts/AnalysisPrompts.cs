namespace Unity.GrantManager.AI
{
    internal static class AnalysisPrompts
    {
        public const string ScoreRules = @"HIGH: Application demonstrates strong evidence across most rubric areas with few or no issues.
MEDIUM: Application has some gaps or weaknesses that require reviewer attention.
LOW: Application has significant gaps or risks across key rubric areas.";

        public const string SeverityRules = @"ERROR: Issue that would likely prevent the application from being approved.
WARNING: Issue that could negatively affect the application's approval.
RECOMMENDATION: Reviewer-facing improvement or follow-up consideration.";

        public const string OutputTemplate = @"{
  ""rating"": ""HIGH/MEDIUM/LOW"",
  ""warnings"": [
    {
      ""category"": ""Brief summary of the warning"",
      ""message"": ""Detailed warning message with full context and explanation""
    }
  ],
  ""errors"": [
    {
      ""category"": ""Brief summary of the error"",
      ""message"": ""Detailed error message with full context and explanation""
    }
  ],
  ""summaries"": [
    {
      ""category"": ""Brief summary of the recommendation"",
      ""message"": ""Detailed recommendation with specific actionable guidance""
    }
  ],
  ""dismissed"": []
}";

        public const string Rules = @"- Use only SCHEMA, DATA, ATTACHMENTS, and RUBRIC as evidence.
- Do not invent fields, documents, requirements, or facts.
- Treat missing or empty values as findings only when they weaken rubric evidence.
- Prefer material issues; avoid nitpicking.
- Each error/warning/recommendation must describe one concrete issue or consideration and why it matters.
- Use 3-6 words for category.
- Each message must be 1-2 complete sentences.
- Each message must be grounded in concrete evidence from provided inputs.
- If attachment evidence is used, reference the attachment explicitly in the message.
- Do not provide applicant-facing advice.
- Do not mention rubric section names in findings.
- If no findings exist, return empty arrays.
- rating must be HIGH, MEDIUM, or LOW.
- Return values exactly as specified in OUTPUT.
- Do not return keys outside OUTPUT.
- Return valid JSON only.
- Return plain JSON only (no markdown).";

        public static readonly string SystemPrompt = PromptHeader.Build(
            "You are an expert grant analyst assistant for human reviewers.",
            "Using SCHEMA, DATA, ATTACHMENTS, RUBRIC, SEVERITY, SCORE, OUTPUT, and RULES, return review findings.");

        public static string BuildUserPrompt(
            string schemaJson,
            string dataJson,
            string attachmentsJson,
            string rubric)
        {
            return $@"SCHEMA
{schemaJson}

DATA
{dataJson}

ATTACHMENTS
{attachmentsJson}

RUBRIC
{rubric}

SEVERITY
{SeverityRules}

SCORE
{ScoreRules}

OUTPUT
{OutputTemplate}

RULES
{Rules}";
        }
    }
}
