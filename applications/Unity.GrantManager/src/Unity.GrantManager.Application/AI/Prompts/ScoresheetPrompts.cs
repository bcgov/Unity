namespace Unity.GrantManager.AI
{
    internal static class ScoresheetPrompts
    {
        public static readonly string SectionSystemPrompt = PromptHeader.Build(
            "You are an expert grant application reviewer for the BC Government.",
            @"Analyze the provided application and generate appropriate answers for the scoresheet section questions based on the application content.
Be thorough, objective, and fair in your assessment. Base your answers strictly on the provided application content.
Always provide citations that reference specific parts of the application content to support your answers.
Be honest about your confidence level - if information is missing or unclear, reflect this in a lower confidence score.
Respond only with valid JSON in the exact format requested.");

        public const string SectionOutputTemplate = @"{
  ""<question_id>"": {
    ""answer"": ""<string | number>"",
    ""citation"": ""<evidence-based rationale>"",
    ""confidence"": 85
  }
}";

        public const string SectionRules = @"Please analyze this grant application and provide appropriate answers for each question in the requested section only.

For each question, provide:
1. Your answer based on the application content
2. A brief cited description (1-2 sentences) explaining your reasoning with specific references to the application content
3. A confidence score from 0-100 indicating how confident you are in your answer based on available information

Guidelines for answers:
- For numeric questions, provide a numeric value within the specified range
- For yes/no questions, provide either 'Yes' or 'No'
- For text questions, provide a concise, relevant response
- For select list questions, respond with ONLY the number from the 'number' field (1, 2, 3, etc.) of your chosen option. NEVER return 0 - the lowest valid answer is 1.
- For text area questions, provide a detailed but concise response
- Base your confidence score on how clearly the application content supports your answer

Do not return any markdown formatting, just the JSON by itself";
    }
}



