// This file is part of TenantClientProxy, you can customize it here
// ReSharper disable once CheckNamespace
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Http.Client.ClientProxying;

namespace Unity.TenantManagement;

public partial class TenantClientProxy
{
    public virtual async Task<List<TenantManagerDto>> GetManagersAsync(Guid id)
    {
        return await RequestAsync<List<TenantManagerDto>>(nameof(GetManagersAsync), new ClientProxyRequestTypeValue
        {
            { typeof(Guid), id }
        });
    }

    public virtual async Task AssignManagerAsync(TenantAssignManagerDto managerAssignment)
    {
        await RequestAsync(nameof(AssignManagerAsync), new ClientProxyRequestTypeValue
        {
            { typeof(TenantAssignManagerDto), managerAssignment }
        });
    }

    public virtual async Task<TenantConnectionStringsDto> GetConnectionStringsAsync(Guid id)
    {
        return await RequestAsync<TenantConnectionStringsDto>(nameof(GetConnectionStringsAsync), new ClientProxyRequestTypeValue
        {
            { typeof(Guid), id }
        });
    }

    public virtual async Task UpdateConnectionStringsAsync(Guid id, TenantConnectionStringsDto input)
    {
        await RequestAsync(nameof(UpdateConnectionStringsAsync), new ClientProxyRequestTypeValue
        {
            { typeof(Guid), id },
            { typeof(TenantConnectionStringsDto), input }
        });
    }
}
