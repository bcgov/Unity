namespace Unity.GrantManager.GrantApplications;

public class AIGenerationStatusDto
{
    public AIGenerationRequestDto? GenerationRequest { get; set; }
    public bool IsGenerating { get; set; }
    public int RetryAfterSeconds { get; set; }
}
