using System;

namespace Unity.GrantManager.Assessments;

public static class AIScoringConstants
{
    // Well-known fixed GUID for the AI Scoring Person record (one per tenant)
    public static readonly Guid AiPersonId = new("00000000-0000-0000-0000-000000000001");
    public const string AiOidcSub = "ai-scoring";
    public const string AiDisplayName = "AI Scoring";
    public const string AiBadge = "AI";
}
