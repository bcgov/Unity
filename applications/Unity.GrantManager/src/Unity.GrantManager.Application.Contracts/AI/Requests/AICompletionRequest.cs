namespace Unity.GrantManager.AI
{
    public class AICompletionRequest
    {
        public string UserPrompt { get; set; } = string.Empty;
        public string? SystemPrompt { get; set; }
        public int MaxTokens { get; set; } = 150;
        public double? Temperature { get; set; }
    }
}
