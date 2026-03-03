namespace Unity.GrantManager.AI
{
    internal static class ScoresheetPrompts
    {
        public static readonly string SectionSystemPrompt = PromptHeader.Build(
            "You are an expert grant application reviewer for the BC Government.",
            "Using DATA, ATTACHMENTS, SECTION, RESPONSE, and RULES, answer only the questions in SECTION.");

        public const string SectionOutputTemplate = @"{
  ""<question_id>"": {
    ""answer"": ""<string | number>"",
    ""rationale"": ""<evidence-based rationale>"",
    ""confidence"": 85
  }
}";

        public const string SectionRules = @"- Use only DATA and ATTACHMENTS as evidence.
- Do not invent missing application details.
- Return exactly one answer object per question ID in SECTION.questions.
- Do not omit any question IDs from SECTION.questions.
- Do not add keys that are not question IDs from SECTION.questions.
- Use RESPONSE as the output contract and fill every placeholder value.
- Each answer object must include: answer, rationale, confidence.
- answer type must match question type: Number => numeric; YesNo/SelectList/Text/TextArea => string.
- For yes/no questions, answer must be exactly ""Yes"" or ""No"".
- For numeric questions, answer must be a numeric value within the allowed range.
- For select list questions, answer must be the selected availableOptions.number encoded as a string.
- For select list questions, never return option label text (for example: ""Yes"", ""No"", or ""N/A""); return the option number string.
- For text and text area questions, answer must be concise, grounded in evidence, and non-empty.
- rationale must be 1-2 complete sentences grounded in concrete DATA/ATTACHMENTS evidence.
- For every question, rationale must justify both the selected answer and confidence level based on evidence strength.
- If evidence is insufficient, choose the most conservative valid answer and state uncertainty in rationale.
- confidence must be an integer from 0 to 100.
- Confidence reflects certainty in the selected answer given available evidence, not application quality.
- Return values exactly as specified in RESPONSE.
- Do not return keys outside RESPONSE.
- Return valid JSON only.
- Return plain JSON only (no markdown).";

        public static string BuildSectionUserPrompt(
            string applicationContent,
            string attachmentSummariesText,
            string sectionPayloadJson)
        {
            return $@"DATA
{applicationContent}

ATTACHMENTS
- {attachmentSummariesText}

SECTION
{sectionPayloadJson}

RESPONSE
{SectionOutputTemplate}

RULES
{SectionRules}";
        }
    }
}
