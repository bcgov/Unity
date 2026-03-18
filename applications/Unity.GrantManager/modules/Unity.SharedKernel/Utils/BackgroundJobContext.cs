using System;
using System.Security.Claims;
using Unity.Modules.Shared.Constants;
using Volo.Abp.Auditing;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;

namespace Unity.Modules.Shared.Utils;

/// <summary>
/// Utility for establishing proper execution context for background jobs and message consumers.
/// Sets up tenant, user identity, and audit scope required for entity change tracking.
/// </summary>
public static class BackgroundJobContext
{
    /// <summary>
    /// Sets up complete background job execution context with auditing, tenant, and user identity.
    /// CRITICAL: Must be called AFTER BackgroundJobExecutionContext.Use() to enable forced auditing.
    /// </summary>
    /// <param name="auditingManager">The auditing manager for creating audit scope</param>
    /// <param name="principalAccessor">The current principal accessor for setting user identity</param>
    /// <param name="currentTenant">The current tenant accessor for setting tenant context</param>
    /// <param name="tenantId">The tenant ID to set context for</param>
    /// <param name="userId">Optional user ID. If null, uses BackgroundJobConstants.BackgroundJobPersonId</param>
    /// <returns>IDisposable that restores previous context when disposed (LIFO order)</returns>
    public static IDisposable Set(
        IAuditingManager auditingManager,
        ICurrentPrincipalAccessor principalAccessor,
        ICurrentTenant currentTenant,
        Guid? tenantId,
        Guid? userId = null)
    {
        var effectiveUserId = userId ?? BackgroundJobConstants.BackgroundJobPersonId;

        var claims = new[]
        {
            new Claim(AbpClaimTypes.UserId, effectiveUserId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, effectiveUserId.ToString()), // Standard claim for user ID
            new Claim(AbpClaimTypes.UserName, BackgroundJobConstants.BackgroundJobUserName),
            new Claim(AbpClaimTypes.Email, BackgroundJobConstants.BackgroundJobEmail),
            new Claim(AbpClaimTypes.TenantId, tenantId?.ToString() ?? Guid.Empty.ToString()),
            new Claim(AbpClaimTypes.Name, BackgroundJobConstants.BackgroundJobName)
        };
        
        // Create an authenticated identity (authenticationType must be non-null for IsAuthenticated to be true)
        var identity = new ClaimsIdentity(claims, "BackgroundJob", AbpClaimTypes.UserName, AbpClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);

        // CRITICAL: Set tenant and principal BEFORE starting audit scope
        // This ensures ABP captures correct context when audit scope is created
        var tenantDisposable = currentTenant.Change(tenantId);
        var principalDisposable = principalAccessor.Change(principal);
        
        // NOW start auditing - it will see the correct tenant/user context
        var auditingDisposable = auditingManager.BeginScope();
        
        // Ensure the current audit log has the user ID set for entity change tracking
        if (auditingManager.Current != null)
        {
            auditingManager.Current.Log.UserId = effectiveUserId;
            auditingManager.Current.Log.UserName = BackgroundJobConstants.BackgroundJobUserName;
            auditingManager.Current.Log.TenantId = tenantId;
        }

        // Dispose in LIFO order: last registered is disposed first.
        // audit scope closes first (while tenant/user context is still valid),
        // then principal, then tenant.
        return new CompositeDisposable(auditingDisposable, principalDisposable, tenantDisposable);
    }

    /// <summary>
    /// Private helper class to combine multiple disposables into one
    /// </summary>
    private sealed class CompositeDisposable : IDisposable
    {
        private readonly IDisposable[] _disposables;
        private bool _disposed;

        public CompositeDisposable(params IDisposable[] disposables)
        {
            _disposables = disposables;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                for (int i = _disposables.Length - 1; i >= 0; i--)
                {
                    _disposables[i]?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
