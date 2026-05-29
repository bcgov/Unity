namespace Unity.AI.RateLimit;

public class AIRateLimitStateDto
{
    public int RetryAfterSeconds { get; set; }
    public bool IsGenerating { get; set; }
}
