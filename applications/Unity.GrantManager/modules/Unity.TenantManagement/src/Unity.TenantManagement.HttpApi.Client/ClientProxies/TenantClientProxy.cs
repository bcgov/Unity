// This file is part of TenantClientProxy, you can customize it here
// ReSharper disable once CheckNamespace
using System.Threading.Tasks;
using Volo.Abp.Http.Client.ClientProxying;

namespace Unity.TenantManagement;

public partial class TenantClientProxy
{
    public virtual async Task AssignManagerAsync(TenantAssignManagerDto managerAssignment)
    {
        await RequestAsync(nameof(AssignManagerAsync), new ClientProxyRequestTypeValue
        {
            { typeof(TenantAssignManagerDto), managerAssignment }
        });
    }
}
