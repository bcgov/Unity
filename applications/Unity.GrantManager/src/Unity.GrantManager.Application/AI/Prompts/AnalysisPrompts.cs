namespace Unity.GrantManager.AI
{
    internal static class AnalysisPrompts
    {
        public const string DefaultRubric = @"ELIGIBILITY REQUIREMENTS: Project aligns with program objectives; Applicant is an eligible entity; Budget is reasonable and justified; Timeline is realistic.
COMPLETENESS CHECKS: Required information is present; Supporting materials are provided where applicable; Description is clear.
FINANCIAL REVIEW: Requested amount is within limits; Budget matches scope; Matching funds or contributions are identified.
RISK ASSESSMENT: Applicant capacity; Feasibility; Compliance considerations; Delivery risks.
QUALITY INDICATORS: Clear objectives; Defined beneficiaries; Appropriate approach; Long-term sustainability.";

        public const string ScoreRules = @"HIGH: Application demonstrates strong evidence across most rubric areas with few or no issues.
MEDIUM: Application has some gaps or weaknesses that require reviewer attention.
LOW: Application has significant gaps or risks across key rubric areas.";

        public const string OutputTemplate = @"{
  ""overall_score"": ""HIGH/MEDIUM/LOW"",
  ""warnings"": [
    {
      ""category"": ""Brief summary of the warning"",
      ""message"": ""Detailed warning message with full context and explanation"",
      ""severity"": ""WARNING""
    }
  ],
  ""errors"": [
    {
      ""category"": ""Brief summary of the error"",
      ""message"": ""Detailed error message with full context and explanation"",
      ""severity"": ""ERROR""
    }
  ],
  ""recommendations"": [
    {
      ""category"": ""Brief summary of the recommendation"",
      ""message"": ""Detailed recommendation with specific actionable guidance""
    }
  ]
}";

        public const string Rules = @"Important: The 'category' field should be a concise summary (3-6 words) that captures the essence of the issue, while the 'message' field should contain the detailed explanation.";

        public static readonly string SystemPrompt = PromptHeader.Build(
            "You are an expert grant application reviewer for the BC Government.",
            @"Conduct a thorough, comprehensive analysis across all rubric categories. Identify substantive issues, concerns, and opportunities for improvement.

Classify findings based on their impact on the application's evaluation and fundability:
- ERRORS: Important missing information, significant gaps in required content, compliance issues, or major concerns affecting eligibility
- WARNINGS: Areas needing clarification, moderate issues, or concerns that should be addressed

Evaluate the quality, clarity, and appropriateness of all application content. Be thorough but fair - identify real issues while avoiding nitpicking.

Respond only with valid JSON in the exact format requested.");
    }
}
