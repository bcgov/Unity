using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Modules.Shared.Constants;
using Unity.Modules.Shared.Utils;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;

namespace Unity.Modules.Shared.Auditing;

/// <summary>
/// Custom auditing helper that forces audit logging for background job operations.
/// Wraps ABP's default AuditingHelper to intercept auditing decisions and ensure
/// EntityChanges are recorded even when no authenticated user is present.
/// </summary>
public class UnityAuditingHelper : IAuditingHelper, ITransientDependency
{
    private readonly AuditingHelper _inner;

    public UnityAuditingHelper(AuditingHelper inner)
    {
        _inner = inner;
    }

    public bool ShouldSaveAudit(MethodInfo? methodInfo, bool defaultValue = false, bool ignoreIntegrationServiceAttribute = false)
    {
        // Force auditing for background jobs - bypass normal checks that fail when currentUser.Id is null
        if (BackgroundJobExecutionContext.IsActive)
        {
            return true;
        }

        return _inner.ShouldSaveAudit(methodInfo, defaultValue, ignoreIntegrationServiceAttribute);
    }

    public bool IsEntityHistoryEnabled(Type entityType, bool defaultValue = false)
    {
        // Force entity history for background jobs - ensures EntityChanges table gets populated
        if (BackgroundJobExecutionContext.IsActive)
        {
            return true;
        }

        return _inner.IsEntityHistoryEnabled(entityType, defaultValue);
    }

    public AuditLogInfo CreateAuditLogInfo()
    {
        var auditLogInfo = _inner.CreateAuditLogInfo();
        
        // Enrich audit log with background job user when no authenticated user present
        if (BackgroundJobExecutionContext.IsActive && auditLogInfo.UserId == null)
        {
            auditLogInfo.UserId = BackgroundJobConstants.BackgroundJobPersonId;
            auditLogInfo.UserName = BackgroundJobConstants.BackgroundJobUserName;
        }
        
        return auditLogInfo;
    }

    public AuditLogActionInfo CreateAuditLogAction(
        AuditLogInfo auditLog,
        Type? type,
        MethodInfo method,
        object?[] arguments)
    {
        return _inner.CreateAuditLogAction(auditLog, type, method, arguments);
    }

    public AuditLogActionInfo CreateAuditLogAction(
        AuditLogInfo auditLog,
        Type? type,
        MethodInfo method,
        IDictionary<string, object?> arguments)
    {
        return _inner.CreateAuditLogAction(auditLog, type, method, arguments);
    }
}

