namespace Unity.AI.Generation;

public class AIGenerationStatusDto
{
    public object? GenerationRequest { get; set; }

    public bool IsGenerating { get; set; }

    public int RetryAfterSeconds { get; set; }
}
