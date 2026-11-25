using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace Unity.AI.Setup;

public static class AISetupConsts
{
    public const string OpenAIProvider = "OpenAI";

    public const int NameMaxLength = 128;

    /// <summary>Providers the runtime can call. Only Azure OpenAI is supported.</summary>
    public static readonly string[] SupportedProviders = [OpenAIProvider];

    /// <summary>Names of Unity.AI.Operations.AIExecutionMode, kept in sync by a unit test.</summary>
    public static readonly string[] ExecutionModes = ["Sequential", "Parallel", "Batch"];
}

public class AIModelDto : AuditedEntityDto<Guid>
{
    /// <summary>The Azure OpenAI deployment name.</summary>
    public string Name { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    /// <summary>Names of the AI operations that point at this model.</summary>
    public List<string> UsedByOperations { get; set; } = [];
}

public class CreateUpdateAIModelDto
{
    [Required]
    [StringLength(AISetupConsts.NameMaxLength)]
    [DisplayName("AISetup:DeploymentName")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [DisplayName("AISetup:Provider")]
    public string Provider { get; set; } = AISetupConsts.OpenAIProvider;

    [DisplayName("AISetup:IsActive")]
    public bool IsActive { get; set; } = true;
}

public class AIModelTestResultDto
{
    public bool Success { get; set; }

    public int? HttpStatusCode { get; set; }

    /// <summary>Azure error code from the response body, such as DeploymentNotFound.</summary>
    public string? ErrorCode { get; set; }

    public long LatencyMilliseconds { get; set; }

    public string Message { get; set; } = string.Empty;
}
