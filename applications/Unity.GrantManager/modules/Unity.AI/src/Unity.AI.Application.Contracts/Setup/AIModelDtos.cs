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

    public const string ChatModelType = "Chat";

    public const string EmbeddingModelType = "Embedding";

    /// <summary>AI Setup row for AI Reporting's chat model (UAI writes SQL with it).</summary>
    public const string AIReportingChatOperation = "AIReporting";

    /// <summary>AI Setup row for AI Reporting's embedding model (UAI matches questions to tables with it).</summary>
    public const string AIReportingEmbeddingOperation = "AIReportingEmbedding";

    /// <summary>AI Reporting's rows: configured in their own AI Setup section, not with the prompt operations.</summary>
    public static readonly string[] AIReportingOperations = [AIReportingChatOperation, AIReportingEmbeddingOperation];

    /// <summary>Names of Unity.AI.Domain.AIModelType, kept in sync by a unit test.</summary>
    public static readonly string[] ModelTypes = [ChatModelType, EmbeddingModelType];

    /// <summary>Names of Unity.AI.Operations.AIExecutionMode, kept in sync by a unit test.</summary>
    public static readonly string[] ExecutionModes = ["Sequential", "Parallel", "Batch"];
}

public class AIModelDto : AuditedEntityDto<Guid>
{
    /// <summary>The Azure OpenAI deployment name.</summary>
    public string Name { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    /// <summary>Chat or Embedding (<see cref="AISetupConsts.ModelTypes"/>).</summary>
    public string Type { get; set; } = AISetupConsts.ChatModelType;

    /// <summary>Azure OpenAI API version sent with calls to this deployment (?api-version=).</summary>
    public string ApiVersion { get; set; } = string.Empty;

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

    [Required]
    [DisplayName("AISetup:Type")]
    public string Type { get; set; } = AISetupConsts.ChatModelType;

    [Required]
    [StringLength(32)]
    [DisplayName("AISetup:ApiVersion")]
    public string ApiVersion { get; set; } = string.Empty;

    [DisplayName("AISetup:IsActive")]
    public bool IsActive { get; set; } = true;
}

/// <summary>Result of calling the Azure OpenAI endpoint's /healthz from the server.</summary>
public class AIEndpointStatusDto
{
    public const string Healthy = "Healthy";
    public const string NoHealthCheck = "NoHealthCheck";
    public const string Unreachable = "Unreachable";
    public const string NotConfigured = "NotConfigured";

    public string? Endpoint { get; set; }

    /// <summary>Healthy, NoHealthCheck (reachable, no /healthz), Unreachable or NotConfigured.</summary>
    public string Status { get; set; } = NotConfigured;

    public int? HttpStatusCode { get; set; }

    /// <summary>The /healthz text (such as which Azure account the proxy forwards to), or the error.</summary>
    public string Message { get; set; } = string.Empty;
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
