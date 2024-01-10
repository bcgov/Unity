using AutoMapper;
using Unity.TenantManagement.Web.Pages.TenantManagement.Tenants;

namespace Unity.TenantManagement.Web;

public class AbpTenantManagementWebAutoMapperProfile : Profile
{
    public AbpTenantManagementWebAutoMapperProfile()
    {
        //List
        CreateMap<TenantDto, EditModalModel.TenantInfoModel>();

        //CreateModal
        CreateMap<CreateModalModel.TenantInfoModel, TenantCreateDto>()            
            .MapExtraProperties();

        //EditModal
        CreateMap<EditModalModel.TenantInfoModel, TenantUpdateDto>()
            .MapExtraProperties();
    }
}
