using Unity.Modules.Shared.Constants;
using Unity.Modules.Shared.Utils;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Timing;
using Volo.Abp.Users;

namespace Unity.Modules.Shared.Auditing;

/// <summary>
/// Custom audit property setter that ensures background jobs have proper user context.
/// With proper BackgroundJobContext setup, ABP should populate most values automatically.
/// This provides a safety net fallback using reflection for readonly properties.
/// </summary>
public class BackgroundJobAuditPropertySetter : AuditPropertySetter, ITransientDependency
{
    public BackgroundJobAuditPropertySetter(ICurrentUser currentUser, ICurrentTenant currentTenant, IClock clock)
        : base(currentUser, currentTenant, clock)
    {
    }

    public override void SetCreationProperties(object targetObject)
    {
        // Call base first to let ABP try to set properties
        base.SetCreationProperties(targetObject);

        // If in background job context and ABP hasn't set creator, use background job user
        if (BackgroundJobExecutionContext.IsActive && 
            targetObject is ICreationAuditedObject createdObject && 
            createdObject.CreatorId == null)
        {
            var propertyInfo = targetObject.GetType().GetProperty(nameof(ICreationAuditedObject.CreatorId));
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(targetObject, BackgroundJobConstants.BackgroundJobPersonId);
            }
        }
    }

    public override void SetModificationProperties(object targetObject)
    {
        // Call base first to let ABP try to set properties  
        base.SetModificationProperties(targetObject);

        // If in background job context and ABP hasn't set modifier, use background job user
        if (BackgroundJobExecutionContext.IsActive && 
            targetObject is IModificationAuditedObject modifiedObject && 
            modifiedObject.LastModifierId == null)
        {
            var propertyInfo = targetObject.GetType().GetProperty(nameof(IModificationAuditedObject.LastModifierId));
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(targetObject, BackgroundJobConstants.BackgroundJobPersonId);
            }
        }
    }
}