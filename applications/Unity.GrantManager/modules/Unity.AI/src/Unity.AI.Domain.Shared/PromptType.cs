namespace Unity.AI;

/// <summary>Categorises what role an AI prompt plays in the system.</summary>
public enum PromptType
{
    Orchestrator = 0,
    Skill = 1,
    Instruction = 2,
    Agent = 3
}
