using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.AI.Setup;

public class AIOperationDto
{
    /// <summary>Null when the operation has no row yet (not configured).</summary>
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsConfigured { get; set; }

    public bool IsBuiltIn { get; set; }

    public Guid? AIModelId { get; set; }

    public string? AIModelName { get; set; }

    public bool AIModelIsActive { get; set; }

    public string ExecutionMode { get; set; } = AISetupConsts.ExecutionModes[0];

    /// <summary>False when the operation ignores its execution mode (it makes a single call).</summary>
    public bool UsesExecutionMode { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Highest active host prompt version, such as "v3", or null when there is none.</summary>
    public string? PromptVersion { get; set; }

    public DateTime? LastModificationTime { get; set; }
}

public class UpdateAIOperationDto
{
    [Required]
    [DisplayName("AISetup:Model")]
    public Guid AIModelId { get; set; }

    [Required]
    [DisplayName("AISetup:ExecutionMode")]
    public string ExecutionMode { get; set; } = AISetupConsts.ExecutionModes[0];

    [DisplayName("AISetup:IsActive")]
    public bool IsActive { get; set; } = true;
}
