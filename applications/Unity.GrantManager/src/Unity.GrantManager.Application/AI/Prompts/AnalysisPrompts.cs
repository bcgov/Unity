namespace Unity.GrantManager.AI
{
    internal static class AnalysisPrompts
    {
        public const string DefaultRubric = @"BC GOVERNMENT GRANT EVALUATION RUBRIC:

1. ELIGIBILITY REQUIREMENTS:
   - Project must align with program objectives
   - Applicant must be eligible entity type
   - Budget must be reasonable and well-justified
   - Project timeline must be realistic

2. COMPLETENESS CHECKS:
   - All required fields completed
   - Necessary supporting documents provided
   - Budget breakdown detailed and accurate
   - Project description clear and comprehensive

3. FINANCIAL REVIEW:
   - Requested amount is within program limits
   - Budget is reasonable for scope of work
   - Matching funds or in-kind contributions identified
   - Cost per outcome/beneficiary is reasonable

4. RISK ASSESSMENT:
   - Applicant capacity to deliver project
   - Technical feasibility of proposed work
   - Environmental or regulatory compliance
   - Potential for cost overruns or delays

5. QUALITY INDICATORS:
   - Clear project objectives and outcomes
   - Well-defined target audience/beneficiaries
   - Appropriate project methodology
   - Sustainability plan for long-term impact

EVALUATION CRITERIA:
- HIGH: Meets all requirements, well-prepared application, low risk
- MEDIUM: Meets most requirements, minor issues or missing elements
- LOW: Missing key requirements, significant concerns, high risk";

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
      ""title"": ""Brief summary of the warning"",
      ""detail"": ""Detailed warning message with full context and explanation""
    }
  ],
  ""errors"": [
    {
      ""title"": ""Brief summary of the error"",
      ""detail"": ""Detailed error message with full context and explanation""
    }
  ],
  ""summaries"": [
    {
      ""title"": ""Brief summary of the recommendation"",
      ""detail"": ""Detailed recommendation with specific actionable guidance""
    }
  ],
  ""dismissed"": []
}";

        public const string Rules = @"- Use only SCHEMA, DATA, ATTACHMENTS, and RUBRIC as evidence.
- Do not invent fields, documents, requirements, or facts.
- Treat missing or empty values as findings only when they weaken rubric evidence.
- Prefer material issues; avoid nitpicking.
- Each error/warning/recommendation must describe one concrete issue or consideration and why it matters.
- Use 3-6 words for title.
- Each detail must be 1-2 complete sentences.
- Each detail must be grounded in concrete evidence from provided inputs.
- If attachment evidence is used, reference the attachment explicitly in detail.
- Do not provide applicant-facing advice.
- Do not mention rubric section names in findings.
- If no findings exist, return empty arrays.
- rating must be HIGH, MEDIUM, or LOW."
            + "\n" + PromptCoreRules.ExactOutputShape
            + "\n" + PromptCoreRules.NoExtraOutputKeys
            + "\n" + PromptCoreRules.ValidJsonOnly
            + "\n" + PromptCoreRules.PlainJsonOnly;

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
