namespace Unity.GrantManager.AI
{
    internal static class AnalysisPrompts
    {
        public const string DefaultRubricV0 = @"ELIGIBILITY REQUIREMENTS: Project aligns with program objectives; Applicant is an eligible entity; Budget is reasonable and justified; Timeline is realistic.
COMPLETENESS CHECKS: Required information is present; Supporting materials are provided where applicable; Description is clear.
FINANCIAL REVIEW: Requested amount is within limits; Budget matches scope; Matching funds or contributions are identified.
RISK ASSESSMENT: Applicant capacity; Feasibility; Compliance considerations; Delivery risks.
QUALITY INDICATORS: Clear objectives; Defined beneficiaries; Appropriate approach; Long-term sustainability.";

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

        public const string OutputTemplateV0 = @"{
  ""rating"": ""<HIGH|MEDIUM|LOW>"",
  ""errors"": [
    {
      ""title"": ""<string>"",
      ""detail"": ""<string>""
    }
  ],
  ""warnings"": [
    {
      ""title"": ""<string>"",
      ""detail"": ""<string>""
    }
  ],
  ""summaries"": [
    {
      ""title"": ""<string>"",
      ""detail"": ""<string>""
    }
  ]
}";

        public const string RulesV0 = PromptCoreRules.UseProvidedEvidence + "\n"
            + "- Do not invent fields, documents, requirements, or facts.\n"
            + @"- Treat missing or empty values as findings only when they weaken rubric evidence.
- Prefer material issues; avoid nitpicking.
- Use 3-6 words for title.
- Each detail must be 1-2 complete sentences.
- Each detail must cite concrete evidence from DATA or ATTACHMENTS.
- If ATTACHMENTS evidence is used, cite the attachment by name in detail.
- If no findings exist, return empty arrays.
- Rating must be HIGH, MEDIUM, or LOW.
"
            + PromptCoreRules.MinimumNarrativeWords + "\n"
            + PromptCoreRules.ExactOutputShape + "\n"
            + PromptCoreRules.NoExtraOutputKeys + "\n"
            + PromptCoreRules.ValidJsonOnly + "\n"
            + PromptCoreRules.PlainJsonOnly;

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
- rating must be HIGH, MEDIUM, or LOW."
            + "\n" + PromptCoreRules.ExactOutputShape
            + "\n" + PromptCoreRules.NoExtraOutputKeys
            + "\n" + PromptCoreRules.ValidJsonOnly
            + "\n" + PromptCoreRules.PlainJsonOnly;

        public static readonly string SystemPrompt = PromptHeader.Build(
            "You are an expert grant analyst assistant for human reviewers.",
            "Using SCHEMA, DATA, ATTACHMENTS, RUBRIC, SEVERITY, SCORE, OUTPUT, and RULES, return review findings.");

        public static readonly string SystemPromptV0 = PromptHeader.Build(
            "You are an expert grant analyst assistant for human reviewers.",
            "Using SCHEMA, DATA, ATTACHMENTS, RUBRIC, SCORE, OUTPUT, and RULES, return review findings.");

        public static string GetRubric(bool useV0) => useV0 ? DefaultRubricV0 : DefaultRubric;
        public static string GetSystemPrompt(bool useV0) => useV0 ? SystemPromptV0 : SystemPrompt;

        public static string BuildUserPrompt(
            string schemaJson,
            string dataJson,
            string attachmentsJson,
            string rubric)
        {
            return BuildUserPrompt(schemaJson, dataJson, attachmentsJson, rubric, useV0: false);
        }

        public static string BuildUserPrompt(
            string schemaJson,
            string dataJson,
            string attachmentsJson,
            string rubric,
            bool useV0)
        {
            var output = useV0 ? OutputTemplateV0 : OutputTemplate;
            var rules = useV0 ? RulesV0 : Rules;
            var severitySection = useV0 ? string.Empty : $@"SEVERITY
{SeverityRules}

";

            return $@"SCHEMA
{schemaJson}

DATA
{dataJson}

ATTACHMENTS
{attachmentsJson}

RUBRIC
{rubric}

{severitySection}SCORE
{ScoreRules}

OUTPUT
{output}

RULES
{rules}";
        }
    }
}
