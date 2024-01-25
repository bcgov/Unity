using AutoMapper;
using Volo.Abp.TenantManagement;

namespace Unity.TenantManagement;

public class UnityTenantManagementApplicationAutoMapperProfile : Profile
{
    public UnityTenantManagementApplicationAutoMapperProfile()
    {
        CreateMap<Tenant, TenantDto>()
            .MapExtraProperties();
    }
}

