namespace Unity.AI.Generation;

public class AIGenerationStatusDto
{
    public AIGenerationStatusRequestDto? GenerationRequest { get; set; }

    public string? FailureReason { get; set; }

    public bool IsGenerating { get; set; }

    public int RetryAfterSeconds { get; set; }
}
